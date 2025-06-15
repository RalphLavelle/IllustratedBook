using IllustratedBook.Services;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

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
        public string? BookId { get; set; }
        
        [FromRoute]
        public int ChapterId { get; set; }

        [FromRoute]
        public int PageId { get; set; }

        public void OnGet()
        {
            if (!string.IsNullOrEmpty(BookId))
            {
                var book = _bookService.GetBook(BookId);
                var chapter = book?.Chapters.FirstOrDefault(c => c.Index == ChapterId);
                if (chapter != null && PageId < chapter.Pages.Count)
                {
                    Page = chapter.Pages[PageId];
                }
            }
        }
    }
} 