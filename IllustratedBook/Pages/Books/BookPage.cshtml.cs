using IllustratedBook.Services;
using IllustratedBook.Models;
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

        public List<string>? Page { get; set; }

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
            // Get the section (chapter) by ID
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
                            if (pages != null && PageId < pages.Count)
                            {
                                Page = pages[PageId];
                            }
                        }
                        catch
                        {
                            // If parsing fails and it's the first page, use the content as is
                            if (PageId == 0)
                            {
                                Page = new List<string> { section.Content };
                            }
                        }
                    }
                }
            }
        }
    }
} 