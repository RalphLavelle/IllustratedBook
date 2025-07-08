using IllustratedBook.Services;
using IllustratedBook.Models;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net;

namespace IllustratedBook.Pages.Books
{
    public class ChapterModel : PageModel
    {
        private readonly BookService _bookService;
        private readonly ChatService _chatService;
        private readonly ImageService _imageService;
        private readonly IConfiguration _configuration;

        public ChapterModel(BookService bookService, ChatService chatService, ImageService imageService, IConfiguration configuration)
        {
            _bookService = bookService;
            _chatService = chatService;
            _imageService = imageService;
            _configuration = configuration;
        }

        public Section? Section { get; set; }
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

        public async Task OnGetAsync()
        {
            // First, try to get the chapter directly from JSON
            // ChapterId should correspond to the chapter index (1-based)
            var chapterIndex = ChapterId - 1; // Convert to 0-based index
            Chapter = _bookService.GetChapterFromJson(BookId, chapterIndex);
            
            if (Chapter != null)
            {
                // Use the chapter data directly from JSON
                Pages = Chapter.Pages;
                
                // Set the current page content based on PageId parameter
                if (Pages != null && Pages.Any())
                {
                    // Default to page 1 if no PageId is specified
                    CurrentPageNumber = PageId ?? 1;
                    
                    // Ensure PageId is within valid range
                    if (CurrentPageNumber < 1) CurrentPageNumber = 1;
                    if (CurrentPageNumber > Pages.Count) CurrentPageNumber = Pages.Count;
                    
                    // Set the current page content
                    CurrentPage = Pages[CurrentPageNumber - 1];
                }
                
                // Create a Section object for compatibility with existing views
                var book = await _bookService.GetBookByIdAsync(BookId);
                Section = new Section
                {
                    SectionId = ChapterId,
                    BookId = BookId,
                    Title = Chapter.Title,
                    Content = JsonSerializer.Serialize(Chapter.Pages),
                    CreatedAt = DateTime.UtcNow,
                    Book = book
                };
                
                // Verify the slugs match
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
            else
            {
                // Fallback to database method if JSON doesn't exist
                Section = await _bookService.GetSectionByIdAsync(ChapterId);
                
                // Verify the slugs match and the section belongs to the correct book
                if (Section != null)
                {
                    var generatedBookSlug = _bookService.GenerateSlug(Section.Book.Title ?? "");
                    var generatedChapterSlug = _bookService.GenerateSlug(Section.Title);
                    
                    if (Section.BookId != BookId || generatedChapterSlug != ChapterSlug)
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
                    
                    // Parse the content as pages if it exists
                    if (!string.IsNullOrEmpty(Section.Content))
                    {
                        try
                        {
                            Pages = JsonSerializer.Deserialize<List<List<string>>>(Section.Content);
                            if (Pages != null && Pages.Any())
                            {
                                // Set the current page content based on PageId parameter
                                CurrentPageNumber = PageId ?? 1;
                                
                                // Ensure PageId is within valid range
                                if (CurrentPageNumber < 1) CurrentPageNumber = 1;
                                if (CurrentPageNumber > Pages.Count) CurrentPageNumber = Pages.Count;
                                
                                // Set the current page content
                                CurrentPage = Pages[CurrentPageNumber - 1];
                            }
                        }
                        catch
                        {
                            // If parsing fails, create a single page with the content
                            Pages = new List<List<string>> { new List<string> { Section.Content } };
                            CurrentPage = Pages[0];
                            CurrentPageNumber = 1;
                        }
                    }
                    else
                    {
                        Pages = new List<List<string>>();
                    }
                }
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
                // Check if image generation is enabled
                if (!IsImageGenerationEnabled())
                {
                    return new JsonResult(new { success = false, error = "Image generation is disabled" });
                }
                
                // Check if we have current page content
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