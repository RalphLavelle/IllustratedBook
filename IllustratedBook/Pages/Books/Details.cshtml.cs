using IllustratedBook.Services;
using IllustratedBook.Models;
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

        public Book? Book { get; set; }
        public IEnumerable<Section>? Sections { get; set; }
        
        [FromRoute]
        public int BookId { get; set; }

        [FromRoute]
        public string? BookSlug { get; set; }

        public async Task OnGetAsync()
        {
            // Get the book by ID
            Book = await _bookService.GetBookByIdAsync(BookId);
            
            // Verify the slug matches (for SEO and security)
            if (Book != null)
            {
                var generatedSlug = _bookService.GenerateSlug(Book.Title ?? "");
                if (generatedSlug != BookSlug)
                {
                    // Redirect to the correct URL if slug doesn't match
                    RedirectToPage(new { bookId = BookId, bookSlug = generatedSlug });
                    return;
                }
                
                // Get the sections (chapters) for this book
                Sections = await _bookService.GetBookSectionsAsync(BookId);
            }
        }
    }
} 