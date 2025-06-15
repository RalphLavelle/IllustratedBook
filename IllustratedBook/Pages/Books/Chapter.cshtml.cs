using IllustratedBook.Services;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace IllustratedBook.Pages.Books
{
    public class ChapterModel : PageModel
    {
        private readonly BookService _bookService;

        public ChapterModel(BookService bookService)
        {
            _bookService = bookService;
        }

        public ChapterViewModel? Chapter { get; set; }

        [FromRoute]
        public string? BookId { get; set; }
        
        [FromRoute]
        public int ChapterId { get; set; }

        public void OnGet()
        {
            if (!string.IsNullOrEmpty(BookId))
            {
                var book = _bookService.GetBook(BookId);
                Chapter = book?.Chapters.FirstOrDefault(c => c.Index == ChapterId);
            }
        }
    }
} 