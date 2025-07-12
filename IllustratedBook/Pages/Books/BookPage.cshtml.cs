using IllustratedBook.Services;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net;
using IllustratedBook.Models;

namespace IllustratedBook.Pages.Books
{
    public class BookPageModel : PageModel
    {
        private readonly BookService _bookService;
        private readonly ChatService _chatService;
        private readonly ImageService _imageService;
        private readonly ImageStorageService _imageStorageService;
        private readonly IConfiguration _configuration;

        public BookPageModel(BookService bookService, ChatService chatService, ImageService imageService, ImageStorageService imageStorageService, IConfiguration configuration)
        {
            _bookService = bookService;
            _chatService = chatService;
            _imageService = imageService;
            _imageStorageService = imageStorageService;
            _configuration = configuration;
        }

        public List<string>? CurrentPage { get; set; }
        public ChapterViewModel? Chapter { get; set; }
        public Image? ExistingImage { get; set; }

        [FromRoute]
        public int BookId { get; set; }

        [FromRoute]
        public string? BookSlug { get; set; }
        
        [FromRoute]
        public int ChapterId { get; set; }

        [FromRoute]
        public string? ChapterSlug { get; set; }

        [FromRoute]
        public int PageId { get; set; }

        public async Task OnGetAsync()
        {
            // Load page content immediately without waiting for image generation
            await LoadPageContentAsync();
            
            // Check if an image already exists for this page
            await CheckForExistingImageAsync();
            
            // Note: Image generation will be handled asynchronously via JavaScript
            // This allows the page content to display immediately
        }

        /// <summary>
        /// Loads the page content without generating images
        /// This method runs synchronously to ensure fast page loading
        /// </summary>
        private async Task LoadPageContentAsync()
        {
            Console.WriteLine($"LoadPageContentAsync: Starting with BookId={BookId}, ChapterId={ChapterId}, PageId={PageId}");
            
            // First, try to get the chapter directly from JSON
            var chapterIndex = ChapterId - 1; // Convert to 0-based index
            Chapter = _bookService.GetChapterFromJson(BookId, chapterIndex);
            
            Console.WriteLine($"LoadPageContentAsync: Chapter from JSON is null: {Chapter == null}");
            
            if (Chapter != null)
            {
                Console.WriteLine($"LoadPageContentAsync: Chapter title: {Chapter.Title}");
                Console.WriteLine($"LoadPageContentAsync: Chapter pages count: {Chapter.Pages?.Count ?? 0}");
                
                // Verify the slug matches
                var generatedChapterSlug = _bookService.GenerateSlug(Chapter.Title ?? "");
                Console.WriteLine($"LoadPageContentAsync: Generated slug: {generatedChapterSlug}, Expected slug: {ChapterSlug}");
                
                if (generatedChapterSlug == ChapterSlug)
                {
                    // Get the specific page from the chapter
                    if (PageId <= Chapter.Pages.Count)
                    {
                        CurrentPage = Chapter.Pages[PageId - 1];
                        Console.WriteLine($"LoadPageContentAsync: Set CurrentPage from JSON, count: {CurrentPage?.Count ?? 0}");
                    }
                    else
                    {
                        Console.WriteLine($"LoadPageContentAsync: PageId {PageId} is out of range for Chapter.Pages.Count {Chapter.Pages.Count}");
                    }
                }
                else
                {
                    Console.WriteLine($"LoadPageContentAsync: Slug mismatch - generated: {generatedChapterSlug}, expected: {ChapterSlug}");
                }
            }
            else
            {
                Console.WriteLine($"LoadPageContentAsync: JSON method failed, trying database fallback");
                
                // Fallback to database method if JSON doesn't exist
                var section = await _bookService.GetSectionByIdAsync(ChapterId);
                
                Console.WriteLine($"LoadPageContentAsync: Section from database is null: {section == null}");
                
                // Verify the slugs match and the section belongs to the correct book
                if (section != null && section.BookId == BookId)
                {
                    Console.WriteLine($"LoadPageContentAsync: Section title: {section.Title}");
                    Console.WriteLine($"LoadPageContentAsync: Section content length: {section.Content?.Length ?? 0}");
                    
                    var generatedChapterSlug = _bookService.GenerateSlug(section.Title);
                    Console.WriteLine($"LoadPageContentAsync: Database generated slug: {generatedChapterSlug}, Expected slug: {ChapterSlug}");
                    
                    if (generatedChapterSlug == ChapterSlug)
                    {
                        // Parse the content as pages if it exists
                        if (!string.IsNullOrEmpty(section.Content))
                        {
                            try
                            {
                                var pages = JsonSerializer.Deserialize<List<List<string>>>(section.Content);
                                Console.WriteLine($"LoadPageContentAsync: Parsed pages count: {pages?.Count ?? 0}");
                                
                                if (pages != null && PageId <= pages.Count)
                                {
                                    CurrentPage = pages[PageId - 1];
                                    Console.WriteLine($"LoadPageContentAsync: Set CurrentPage from database, count: {CurrentPage?.Count ?? 0}");
                                }
                                else
                                {
                                    Console.WriteLine($"LoadPageContentAsync: PageId {PageId} is out of range for parsed pages count {pages?.Count ?? 0}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"LoadPageContentAsync: JSON parsing failed: {ex.Message}");
                                // If parsing fails and it's the first page, use the content as is
                                if (PageId == 1)
                                {
                                    CurrentPage = new List<string> { section.Content };
                                    Console.WriteLine($"LoadPageContentAsync: Set CurrentPage from raw content, count: {CurrentPage?.Count ?? 0}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"LoadPageContentAsync: Section content is null or empty");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"LoadPageContentAsync: Database slug mismatch - generated: {generatedChapterSlug}, expected: {ChapterSlug}");
                    }
                }
                else
                {
                    Console.WriteLine($"LoadPageContentAsync: Section is null or doesn't belong to correct book (BookId: {section?.BookId}, Expected: {BookId})");
                }
            }
        }

        /// <summary>
        /// Handles asynchronous image generation via AJAX
        /// This method is called by JavaScript to generate images without blocking the page
        /// </summary>
        /// <returns>JSON result indicating success and image URL</returns>
        public async Task<IActionResult> OnPostGenerateImageAsync()
        {
            try
            {
                // Load page content first
                await LoadPageContentAsync();
                
                // Add debugging information
                Console.WriteLine($"Debug: BookId={BookId}, ChapterId={ChapterId}, PageId={PageId}");
                Console.WriteLine($"Debug: CurrentPage is null: {CurrentPage == null}");
                Console.WriteLine($"Debug: CurrentPage count: {CurrentPage?.Count ?? 0}");
                if (CurrentPage != null)
                {
                    Console.WriteLine($"Debug: CurrentPage content: {string.Join(" | ", CurrentPage)}");
                }
                
                // Check if image generation is enabled
                if (!IsImageGenerationEnabled())
                {
                    return new JsonResult(new { success = false, error = "Image generation is disabled" });
                }
                
                // Check if we have page content
                if (CurrentPage == null || !CurrentPage.Any())
                {
                    return new JsonResult(new { success = false, error = "No page content found" });
                }
                
                // First, check if an image already exists for this page
                var existingImage = await _imageStorageService.GetExistingImageAsync(BookId, ChapterId, PageId);
                
                if (existingImage != null)
                {
                    // Use the existing image instead of generating a new one
                    Console.WriteLine($"Using existing image for Book {BookId}, Chapter {ChapterId}, Page {PageId}");
                    return new JsonResult(new { 
                        success = true, 
                        imageUrl = existingImage.ImageUrl,
                        fromCache = true,
                        prompt = existingImage.Prompt
                    });
                }
                
                // No existing image found, generate a new one
                Console.WriteLine($"No existing image found, generating new image for Book {BookId}, Chapter {ChapterId}, Page {PageId}");
                var imageUrl = await GenerateImageForPageAsync();
                
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    return new JsonResult(new { success = true, imageUrl = imageUrl, fromCache = false });
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
                // Combine all paragraphs into a single text for prompt generation
                var pageText = string.Join(" ", CurrentPage!.Select(p => WebUtility.HtmlDecode(p)));
                
                // Remove HTML tags to get clean text
                pageText = System.Text.RegularExpressions.Regex.Replace(pageText, "<[^>]*>", "");

                // Generate a prompt using the ChatService
                var prompt = await _chatService.GenerateFluxPromptAsync(pageText);

                Console.WriteLine($"Prompt: {prompt}");

                // Generate the image using the ImageService
                var imageUrl = await _imageService.GenerateImageAsync(prompt);
                
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    // Save the generated image to the database for future reuse
                    var saveSuccess = await _imageStorageService.SaveImageAsync(
                        bookId: BookId,
                        chapterId: ChapterId,
                        pageNumber: PageId,
                        prompt: prompt,
                        imageUrl: imageUrl,
                        model: _imageService.GetModelName(), // We'll need to add this method
                        modelVersion: _imageService.GetModelVersion(), // We'll need to add this method
                        width: 1024,
                        height: 1024,
                        inferenceSteps: 20,
                        guidanceScale: 7.5,
                        negativePrompt: "blurry, low quality, distorted, deformed"
                    );
                    
                    if (saveSuccess)
                    {
                        Console.WriteLine($"Image saved to database for Book {BookId}, Chapter {ChapterId}, Page {PageId}");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Failed to save image to database for Book {BookId}, Chapter {ChapterId}, Page {PageId}");
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
        /// Checks if an image already exists for the current page
        /// </summary>
        private async Task CheckForExistingImageAsync()
        {
            try
            {
                // Only check if image generation is enabled
                if (!IsImageGenerationEnabled())
                {
                    return;
                }

                // Check if we have page content
                if (CurrentPage == null || !CurrentPage.Any())
                {
                    return;
                }

                // Look for an existing image for this specific book, chapter, and page
                ExistingImage = await _imageStorageService.GetExistingImageAsync(BookId, ChapterId, PageId);
                
                if (ExistingImage != null)
                {
                    Console.WriteLine($"Found existing image for Book {BookId}, Chapter {ChapterId}, Page {PageId}");
                }
                else
                {
                    Console.WriteLine($"No existing image found for Book {BookId}, Chapter {ChapterId}, Page {PageId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for existing image: {ex.Message}");
                ExistingImage = null;
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