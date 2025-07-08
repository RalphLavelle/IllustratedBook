using IllustratedBook.Services;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net;

namespace IllustratedBook.Pages.Books
{
    public class BookPageModel : PageModel
    {
        private readonly BookService _bookService;
        private readonly ChatService _chatService;
        private readonly ImageService _imageService;
        private readonly IConfiguration _configuration;

        public BookPageModel(BookService bookService, ChatService chatService, ImageService imageService, IConfiguration configuration)
        {
            _bookService = bookService;
            _chatService = chatService;
            _imageService = imageService;
            _configuration = configuration;
        }

        public List<string>? CurrentPage { get; set; }
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
        public int PageId { get; set; }

        public async Task OnGetAsync()
        {
            // Load page content immediately without waiting for image generation
            await LoadPageContentAsync();
            
            // Note: Image generation will be handled asynchronously via JavaScript
            // This allows the page content to display immediately
        }

        /// <summary>
        /// Loads the page content without generating images
        /// This method runs synchronously to ensure fast page loading
        /// </summary>
        private async Task LoadPageContentAsync()
        {
            // First, try to get the chapter directly from JSON
            var chapterIndex = ChapterId - 1; // Convert to 0-based index
            Chapter = _bookService.GetChapterFromJson(BookId, chapterIndex);
            
            if (Chapter != null)
            {
                // Verify the slug matches
                var generatedChapterSlug = _bookService.GenerateSlug(Chapter.Title ?? "");
                if (generatedChapterSlug == ChapterSlug)
                {
                    // Get the specific page from the chapter
                    if (PageId <= Chapter.Pages.Count)
                    {
                        CurrentPage = Chapter.Pages[PageId - 1];
                    }
                }
            }
            else
            {
                // Fallback to database method if JSON doesn't exist
                var section = await _bookService.GetSectionByIdAsync(ChapterId);
                
                // Verify the slugs match and the section belongs to the correct book
                if (section != null && section.BookId == BookId)
                {
                    var generatedChapterSlug = _bookService.GenerateSlug(section.Title);
                    if (generatedChapterSlug == ChapterSlug)
                    {
                        // Parse the content as pages if it exists
                        if (!string.IsNullOrEmpty(section.Content))
                        {
                            try
                            {
                                var pages = JsonSerializer.Deserialize<List<List<string>>>(section.Content);
                                if (pages != null && PageId <= pages.Count)
                                {
                                    CurrentPage = pages[PageId - 1];
                                }
                            }
                            catch
                            {
                                // If parsing fails and it's the first page, use the content as is
                                if (PageId == 1)
                                {
                                    CurrentPage = new List<string> { section.Content };
                                }
                            }
                        }
                    }
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
                
                // Generate the image
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
                // Combine all paragraphs into a single text for prompt generation
                var pageText = string.Join(" ", CurrentPage!.Select(p => WebUtility.HtmlDecode(p)));
                
                // Remove HTML tags to get clean text
                pageText = System.Text.RegularExpressions.Regex.Replace(pageText, "<[^>]*>", "");

                // Generate a prompt using the ChatService
                var prompt = await _chatService.GenerateFluxPromptAsync(pageText);

                Console.WriteLine($"Prompt: {prompt}");

                // Generate the image using the ImageService
                return await _imageService.GenerateImageAsync(prompt);
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