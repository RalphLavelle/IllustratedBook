using IllustratedBook.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

        public ImageStorageService(DataContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
                    .OrderByDescending(i => i.GeneratedAt) // Get the most recent one if multiple exist
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
                // Create a new image record
                var image = new Image
                {
                    BookId = bookId,
                    ChapterId = chapterId,
                    PageNumber = pageNumber,
                    Prompt = prompt,
                    ImageUrl = imageUrl,
                    Model = model,
                    ModelVersion = modelVersion,
                    Width = width,
                    Height = height,
                    InferenceSteps = inferenceSteps,
                    GuidanceScale = guidanceScale,
                    NegativePrompt = negativePrompt,
                    GeneratedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add the image to the database
                _context.Images.Add(image);
                await _context.SaveChangesAsync();

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