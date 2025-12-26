using IllustratedBook.Services;
using IllustratedBook.Models;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net;

namespace IllustratedBook.Pages.Books
{
    // Helper classes for session storage data
    public class SessionPageData
    {
        public int BookId { get; set; }
        public int ChapterId { get; set; }
        public int PageId { get; set; }
        public List<string> PageData { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; }
    }

    public class SessionChapterData
    {
        public int BookId { get; set; }
        public int ChapterId { get; set; }
        public ChapterViewModel? Chapter { get; set; }
        public List<List<string>>? Pages { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ChapterModel : PageModel
    {
        private readonly BookService _bookService;
        private readonly ChatService _chatService;
        private readonly ImageService _imageService;
        private readonly ImageStorageService _imageStorageService;
        private readonly IConfiguration _configuration;

        public ChapterModel(BookService bookService, ChatService chatService, ImageService imageService, ImageStorageService imageStorageService, IConfiguration configuration)
        {
            _bookService = bookService;
            _chatService = chatService;
            _imageService = imageService;
            _imageStorageService = imageStorageService;
            _configuration = configuration;
        }

        public ChapterViewModel? Section { get; set; }
        public List<List<string>>? Pages { get; set; }
        public List<string>? CurrentPage { get; set; }
        public int CurrentPageNumber { get; set; } = 1;
        public ChapterViewModel? Chapter { get; set; }

        [FromRoute]
        public int BookId { get; set; }

        [FromRoute]
        public string? BookSlug { get; set; }
        
        [FromRoute]
        public int ChapterId { get; set; }

        [FromRoute]
        public string? ChapterSlug { get; set; }

        [FromRoute]
        public int? PageId { get; set; }

        /// <summary>
        /// Generates a unique session storage key for the current chapter and page
        /// </summary>
        /// <param name="bookId">The book ID</param>
        /// <param name="chapterId">The chapter ID</param>
        /// <param name="pageId">The page ID (optional)</param>
        /// <returns>A unique key for session storage</returns>
        private string GetSessionStorageKey(int bookId, int chapterId, int? pageId = null)
        {
            return pageId.HasValue 
                ? $"chapter_{bookId}_{chapterId}_page_{pageId.Value}"
                : $"chapter_{bookId}_{chapterId}";
        }

        /// <summary>
        /// Saves the current page data to session storage
        /// </summary>
        /// <param name="bookId">The book ID</param>
        /// <param name="chapterId">The chapter ID</param>
        /// <param name="pageId">The page ID</param>
        /// <param name="pageData">The page data to save</param>
        private void SavePageToSessionStorage(int bookId, int chapterId, int pageId, List<string> pageData)
        {
            try
            {
                var key = GetSessionStorageKey(bookId, chapterId, pageId);
                var data = new SessionPageData
                {
                    BookId = bookId,
                    ChapterId = chapterId,
                    PageId = pageId,
                    PageData = pageData,
                    Timestamp = DateTime.UtcNow
                };
                
                // Store in session for server-side access
                HttpContext.Session.SetString(key, JsonSerializer.Serialize(data));
                
                // Also store the entire chapter data for quick access
                var chapterKey = GetSessionStorageKey(bookId, chapterId);
                if (Chapter != null && Pages != null)
                {
                    var chapterData = new SessionChapterData
                    {
                        BookId = bookId,
                        ChapterId = chapterId,
                        Chapter = Chapter,
                        Pages = Pages,
                        Timestamp = DateTime.UtcNow
                    };
                    HttpContext.Session.SetString(chapterKey, JsonSerializer.Serialize(chapterData));
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the page load
                Console.WriteLine($"Error saving to session storage: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves page data from session storage
        /// </summary>
        /// <param name="bookId">The book ID</param>
        /// <param name="chapterId">The chapter ID</param>
        /// <param name="pageId">The page ID</param>
        /// <returns>The page data if found, null otherwise</returns>
        private List<string>? GetPageFromSessionStorage(int bookId, int chapterId, int pageId)
        {
            try
            {
                var key = GetSessionStorageKey(bookId, chapterId, pageId);
                var sessionData = HttpContext.Session.GetString(key);
                
                if (!string.IsNullOrEmpty(sessionData))
                {
                    var data = JsonSerializer.Deserialize<SessionPageData>(sessionData);
                    if (data?.PageData != null)
                    {
                        return data.PageData;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving from session storage: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves chapter data from session storage
        /// </summary>
        /// <param name="bookId">The book ID</param>
        /// <param name="chapterId">The chapter ID</param>
        /// <returns>The chapter data if found, null otherwise</returns>
        private ChapterViewModel? GetChapterFromSessionStorage(int bookId, int chapterId)
        {
            try
            {
                var key = GetSessionStorageKey(bookId, chapterId);
                var sessionData = HttpContext.Session.GetString(key);
                
                if (!string.IsNullOrEmpty(sessionData))
                {
                    var data = JsonSerializer.Deserialize<SessionChapterData>(sessionData);
                    return data?.Chapter;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving chapter from session storage: {ex.Message}");
                return null;
            }
        }

        public async Task OnGetAsync()
        {
            // Set the current page number first
            CurrentPageNumber = PageId ?? 1;
            if (CurrentPageNumber < 1) CurrentPageNumber = 1;

            // Try to get the current page from session storage first
            var cachedPage = GetPageFromSessionStorage(BookId, ChapterId, CurrentPageNumber);
            if (cachedPage != null)
            {
                // Use cached page data
                CurrentPage = cachedPage;
                
                // Try to get chapter data from session storage
                Chapter = GetChapterFromSessionStorage(BookId, ChapterId);
                if (Chapter != null)
                {
                    Pages = Chapter.Pages;
                }
                
                // Create a ChapterViewModel for the view
                Section = new ChapterViewModel
                {
                    Index = ChapterId,
                    Title = Chapter?.Title ?? $"Chapter {ChapterId}",
                    Pages = Chapter?.Pages ?? new List<List<string>>()
                };
                
                return; // Exit early since we have cached data
            }

            // If not in session storage, get from JSON or database
            var chapterIndex = ChapterId - 1; // Convert to 0-based index
            Chapter = _bookService.GetChapterFromJson(BookId, chapterIndex);
            
            if (Chapter != null)
            {
                // Use the chapter data directly from JSON
                Pages = Chapter.Pages;
                
                // Ensure PageId is within valid range
                if (Pages != null && Pages.Any())
                {
                    if (CurrentPageNumber > Pages.Count) CurrentPageNumber = Pages.Count;
                    
                    // Set the current page content
                    CurrentPage = Pages[CurrentPageNumber - 1];
                    
                    // Save to session storage for future use
                    SavePageToSessionStorage(BookId, ChapterId, CurrentPageNumber, CurrentPage);
                }
                
                // Create a ChapterViewModel for the view
                Section = new ChapterViewModel
                {
                    Index = ChapterId,
                    Title = Chapter.Title ?? $"Chapter {ChapterId}",
                    Pages = Chapter.Pages ?? new List<List<string>>()
                };
                
                // Verify the slugs match
                var book = await _bookService.GetBookByIdAsync(BookId);
                if (book != null)
                {
                    var generatedBookSlug = _bookService.GenerateSlug(book.Title ?? "");
                    var generatedChapterSlug = _bookService.GenerateSlug(Chapter.Title ?? "");
                    
                    if (generatedBookSlug != BookSlug || generatedChapterSlug != ChapterSlug)
                    {
                        // Redirect to the correct URL if slugs don't match
                        RedirectToPage(new { 
                            bookId = BookId, 
                            bookSlug = generatedBookSlug,
                            chapterId = ChapterId, 
                            chapterSlug = generatedChapterSlug 
                        });
                        return;
                    }
                }
            }
            // If chapter not found in JSON, show not found
        }

        /// <summary>
        /// Handles AJAX requests to get page data from session storage
        /// This method is called by JavaScript to check if page data is cached
        /// </summary>
        /// <returns>JSON result with page data if found in session storage</returns>
        public IActionResult OnGetPageFromSessionAsync()
        {
            try
            {
                CurrentPageNumber = PageId ?? 1;
                if (CurrentPageNumber < 1) CurrentPageNumber = 1;

                var pageData = GetPageFromSessionStorage(BookId, ChapterId, CurrentPageNumber);
                
                if (pageData != null)
                {
                    return new JsonResult(new { 
                        success = true, 
                        pageData = pageData,
                        fromCache = true 
                    });
                }
                else
                {
                    return new JsonResult(new { 
                        success = false, 
                        message = "Page data not found in session storage" 
                    });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Handles asynchronous image generation via AJAX for the current page
        /// This method is called by JavaScript to generate images without blocking the page
        /// </summary>
        /// <returns>JSON result indicating success and image URL</returns>
        public async Task<IActionResult> OnPostGenerateImageAsync()
        {
            try
            {
                CurrentPageNumber = PageId ?? 1;
                if (CurrentPageNumber < 1) CurrentPageNumber = 1;

                // Check if image generation is enabled
                if (!IsImageGenerationEnabled())
                {
                    return new JsonResult(new { success = false, error = "Image generation is disabled" });
                }
                
                // First, check if an image already exists for this page
                var existingImage = await _imageStorageService.GetExistingImageAsync(BookId, ChapterId, CurrentPageNumber);
                
                if (existingImage != null)
                {
                    // Prefer a locally stored image under /Images if available
                    Console.WriteLine($"Using existing image for Book {BookId}, Chapter {ChapterId}, Page {CurrentPageNumber}");

                    var (localPath, localUrl) = _imageStorageService.ResolveLocalImagePathAndUrl(BookId, ChapterId, CurrentPageNumber, existingImage.Metadata);
                    string? resolvedUrl = localUrl;

                    // Fallback to remote imageUrl in metadata if local not found
                    if (string.IsNullOrWhiteSpace(resolvedUrl))
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(existingImage.Metadata))
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(existingImage.Metadata);
                                if (doc.RootElement.TryGetProperty("imageUrl", out var urlProp))
                                {
                                    resolvedUrl = urlProp.GetString();
                                }
                            }
                        }
                        catch { }
                    }

                    return new JsonResult(new {
                        success = true,
                        imageUrl = resolvedUrl,
                        fromDatabase = true,
                        fromLocal = !string.IsNullOrWhiteSpace(localUrl)
                    });
                }
                
                // Generate a new image if none exists
                var imageUrl = await GenerateImageForPageAsync();
                
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    return new JsonResult(new { success = true, imageUrl = imageUrl });
                }
                else
                {
                    return new JsonResult(new { success = false, error = "Failed to generate image" });
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Generates an image for the current page using AI services
        /// Returns the image URL instead of setting properties
        /// </summary>
        /// <returns>The URL of the generated image, or null if generation failed</returns>
        private async Task<string?> GenerateImageForPageAsync()
        {
            try
            {
                var pageData = GetPageFromSessionStorage(BookId, ChapterId, CurrentPageNumber);

                // Combine all paragraphs into a single text for prompt generation
                var pageText = string.Join(" ", pageData!.Select(p => WebUtility.HtmlDecode(p)));
                
                // Remove HTML tags to get clean text
                pageText = System.Text.RegularExpressions.Regex.Replace(pageText, "<[^>]*>", "");

                // Generate a prompt using the ChatService
                var prompt = await _chatService.GenerateFluxPromptAsync(pageText);

                Console.WriteLine($"Prompt: {prompt}");

                // Generate the image using the ImageService
                var imageUrl = await _imageService.GenerateImageAsync(prompt);
                
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Store the generated image in the database with metadata
                    var modelName = _imageService.GetModelName();
                    var modelVersion = _imageService.GetModelVersion();
                    
                    // Default settings for Flux model
                    var width = 1024;
                    var height = 1024;
                    var inferenceSteps = 20;
                    var guidanceScale = 7.5;
                    var negativePrompt = "blurry, low quality, distorted, deformed";
                    
                    var saveSuccess = await _imageStorageService.SaveImageAsync(
                        BookId,
                        ChapterId,
                        CurrentPageNumber,
                        prompt,
                        imageUrl,
                        modelName,
                        modelVersion,
                        width,
                        height,
                        inferenceSteps,
                        guidanceScale,
                        negativePrompt
                    );
                    
                    if (saveSuccess)
                    {
                        Console.WriteLine($"Image saved to database for Book {BookId}, Chapter {ChapterId}, Page {CurrentPageNumber}");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Failed to save image to database for Book {BookId}, Chapter {ChapterId}, Page {CurrentPageNumber}");
                    }
                }
                
                return imageUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Image generation error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Checks if image generation is enabled in the configuration
        /// </summary>
        /// <returns>True if image generation should be performed</returns>
        private bool IsImageGenerationEnabled()
        {
            var generateSetting = _configuration["Images:Generate"];
            return bool.TryParse(generateSetting, out var enabled) && enabled;
        }
    }
} 