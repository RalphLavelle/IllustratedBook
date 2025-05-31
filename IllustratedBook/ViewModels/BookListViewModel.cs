using IllustratedBook.Models;

namespace IllustratedBook.ViewModels
{
    public class BookListViewModel
    {
        public IEnumerable<Book> Books { get; set; } = Array.Empty<Book>();

        public string? SelectedBook { get; set; }
    }
}
