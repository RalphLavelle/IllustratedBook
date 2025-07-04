using IllustratedBook.Services;
using IllustratedBook.Models;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace IllustratedBook.Pages.Books
{
    public class BookPageModel : PageModel
    {
        private readonly BookService _bookService;

        public BookPageModel(BookService bookService)
        {
            _bookService = bookService;
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
    }
} 