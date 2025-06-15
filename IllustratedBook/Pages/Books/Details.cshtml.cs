using IllustratedBook.Services;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IllustratedBook.Pages.Books
{
    public class DetailsModel : PageModel
    {
        private readonly BookService _bookService;

        public DetailsModel(BookService bookService)
        {
            _bookService = bookService;
        }

        public BookViewModel? Book { get; set; }
        
        [FromRoute]
        public string? BookId { get; set; }

        public void OnGet()
        {
            if (!string.IsNullOrEmpty(BookId))
            {
                Book = _bookService.GetBook(BookId);
            }
        }
    }
} 