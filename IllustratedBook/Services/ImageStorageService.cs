using IllustratedBook.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;
using System.Net.Http;
using System.IO;

namespace IllustratedBook.Services
{
    /// <summary>
    /// Service for managing image storage in the database
    /// This service handles saving generated images and retrieving existing ones
    /// </summary>
    public class ImageStorageService
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public ImageStorageService(DataContext context, IConfiguration configuration, IWebHostEnvironment environment)
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
        }

        /// <summary>
        /// Computes the absolute path to the top-level Images directory (one level above content root).
        /// </summary>
        private string GetImagesRoot()
        {
            var contentRoot = _environment.ContentRootPath;
            var solutionRoot = Directory.GetParent(contentRoot)?.FullName ?? contentRoot;
            return Path.Combine(solutionRoot, "Images");
        }

        /// <summary>
        /// Ensures the folder for a specific book exists under the Images root and returns the path.
        /// </summary>
        private string EnsureBookImagesFolder(int bookId)
        {
            var imagesRoot = GetImagesRoot();
            var bookFolder = Path.Combine(imagesRoot, bookId.ToString());
            if (!Directory.Exists(bookFolder))
            {
                Directory.CreateDirectory(bookFolder);
            }
            return bookFolder;
        }

        /// <summary>
        /// Saves an image file to the Images/{bookId} folder using chapter and page in the filename.
        /// The filename format is: chapter-{chapterId}_page-{pageNumber}.png
        /// </summary>
        private async Task<(bool success, string? localPath)> SaveImageFileAsync(int bookId, int chapterId, int pageNumber, string imageUrl)
        {
            try
            {
                var bookFolder = EnsureBookImagesFolder(bookId);
                var targetFileName = $"chapter-{chapterId}_page-{pageNumber}.png";
                var targetPath = Path.Combine(bookFolder, targetFileName);

                using var http = new HttpClient();
                var bytes = await http.GetByteArrayAsync(imageUrl);
                await File.WriteAllBytesAsync(targetPath, bytes);
                return (true, targetPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image file locally: {ex.Message}");
                return (false, null);
            }
        }

        /// <summary>
        /// Resolves the local image file path and public URL for the given identifiers.
        /// Tries the new canonical filename first, then falls back to any matching legacy file.
        /// </summary>
        public (string? localPath, string? publicUrl) ResolveLocalImagePathAndUrl(int bookId, int chapterId, int pageNumber, string? metadataJson)
        {
            try
            {
                var imagesRoot = GetImagesRoot();
                var bookFolder = Path.Combine(imagesRoot, bookId.ToString());
                if (!Directory.Exists(bookFolder))
                {
                    return (null, null);
                }

                var preferredName = $"chapter-{chapterId}_page-{pageNumber}.png";
                var preferredPath = Path.Combine(bookFolder, preferredName);
                if (File.Exists(preferredPath))
                {
                    var url = $"/Images/{bookId}/{preferredName}";
                    return (preferredPath, url);
                }

                // Fallback: scan for legacy files and match exact chapter/page via regex to avoid substring collisions
                // Supported extensions: png, jpg, jpeg, webp
                var allFiles = Directory.GetFiles(bookFolder);
                foreach (var file in allFiles)
                {
                    var name = Path.GetFileName(file);
                    // Example matches: chapter-1_page-2.png or chapter-1_page-2_20250101T120000Z.png
                    var m = System.Text.RegularExpressions.Regex.Match(name, @"^chapter-(\d+)_page-(\d+).*(\.png|\.jpg|\.jpeg|\.webp)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        if (int.TryParse(m.Groups[1].Value, out var ch) && int.TryParse(m.Groups[2].Value, out var pg))
                        {
                            if (ch == chapterId && pg == pageNumber)
                            {
                                var url = $"/Images/{bookId}/{name}";
                                return (Path.Combine(bookFolder, name), url);
                            }
                        }
                    }
                }

                return (null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving local image path: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Checks if an image already exists for the specified book, chapter, and page
        /// </summary>
        /// <param name="bookId">The ID of the book</param>
        /// <param name="chapterId">The ID of the chapter</param>
        /// <param name="pageNumber">The page number within the chapter</param>
        /// <returns>The existing image if found, null otherwise</returns>
        public async Task<Image?> GetExistingImageAsync(int bookId, int chapterId, int pageNumber)
        {
            try
            {
                // Look for an existing image for this specific book, chapter, and page
                var existingImage = await _context.Images
                    .Where(i => i.BookId == bookId && 
                               i.ChapterId == chapterId && 
                               i.PageNumber == pageNumber)
                    .OrderByDescending(i => i.CreatedAt) // Get the most recent one if multiple exist
                    .FirstOrDefaultAsync();

                return existingImage;
            }
            catch (Exception ex)
            {
                // Log the error (in a real application, you'd use a proper logging framework)
                Console.WriteLine($"Error retrieving existing image: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves a newly generated image to the database
        /// </summary>
        /// <param name="bookId">The ID of the book</param>
        /// <param name="chapterId">The ID of the chapter</param>
        /// <param name="pageNumber">The page number within the chapter</param>
        /// <param name="prompt">The text prompt used to generate the image</param>
        /// <param name="imageUrl">The URL where the image is stored</param>
        /// <param name="model">The AI model used</param>
        /// <param name="modelVersion">The model version used</param>
        /// <param name="width">The width of the generated image</param>
        /// <param name="height">The height of the generated image</param>
        /// <param name="inferenceSteps">The number of inference steps used</param>
        /// <param name="guidanceScale">The guidance scale used</param>
        /// <param name="negativePrompt">The negative prompt used</param>
        /// <returns>True if the image was saved successfully, false otherwise</returns>
        public async Task<bool> SaveImageAsync(
            int bookId, 
            int chapterId, 
            int pageNumber, 
            string prompt, 
            string imageUrl, 
            string model, 
            string? modelVersion = null,
            int width = 1024, 
            int height = 1024, 
            int inferenceSteps = 20, 
            double guidanceScale = 7.5, 
            string? negativePrompt = null)
        {
            try
            {
                // Build metadata payload to store former discrete fields
                var metadataObject = new
                {
                    imageUrl,
                    model,
                    modelVersion,
                    width,
                    height,
                    inferenceSteps,
                    guidanceScale,
                    negativePrompt,
                    generatedAt = DateTime.UtcNow
                };
                var metadataJson = JsonSerializer.Serialize(metadataObject);

                // Create a new image record
                var image = new Image
                {
                    BookId = bookId,
                    ChapterId = chapterId,
                    PageNumber = pageNumber,
                    Prompt = prompt,
                    Metadata = metadataJson,
                    CreatedAt = DateTime.UtcNow,
                };

                // Add the image to the database
                _context.Images.Add(image);
                await _context.SaveChangesAsync();

                // Save the actual image file to the Images folder using chapter/page-only filename
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    var (fileSaved, localPath) = await SaveImageFileAsync(bookId, chapterId, pageNumber, imageUrl);
                    if (fileSaved)
                    {
                        Console.WriteLine($"Image file saved locally at {localPath}");
                    }
                    else
                    {
                        Console.WriteLine("Warning: Failed to save image file locally.");
                    }
                }

                Console.WriteLine($"Image saved successfully for Book {bookId}, Chapter {chapterId}, Page {pageNumber}");
                return true;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error saving image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all images for a specific book
        /// </summary>
        /// <param name="bookId">The ID of the book</param>
        /// <returns>A list of all images for the book</returns>
        public async Task<List<Image>> GetImagesForBookAsync(int bookId)
        {
            try
            {
                return await _context.Images
                    .Where(i => i.BookId == bookId)
                    .OrderBy(i => i.ChapterId)
                    .ThenBy(i => i.PageNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving images for book {bookId}: {ex.Message}");
                return new List<Image>();
            }
        }

        /// <summary>
        /// Gets all images for a specific chapter
        /// </summary>
        /// <param name="bookId">The ID of the book</param>
        /// <param name="chapterId">The ID of the chapter</param>
        /// <returns>A list of all images for the chapter</returns>
        public async Task<List<Image>> GetImagesForChapterAsync(int bookId, int chapterId)
        {
            try
            {
                return await _context.Images
                    .Where(i => i.BookId == bookId && i.ChapterId == chapterId)
                    .OrderBy(i => i.PageNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving images for chapter {chapterId}: {ex.Message}");
                return new List<Image>();
            }
        }

        /// <summary>
        /// Deletes an image from the database
        /// </summary>
        /// <param name="imageId">The ID of the image to delete</param>
        /// <returns>True if the image was deleted successfully, false otherwise</returns>
        public async Task<bool> DeleteImageAsync(int imageId)
        {
            try
            {
                var image = await _context.Images.FindAsync(imageId);
                if (image != null)
                {
                    _context.Images.Remove(image);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image {imageId}: {ex.Message}");
                return false;
            }
        }
    }
} 