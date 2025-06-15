using System.Text.Json;
using IllustratedBook.ViewModels;

namespace IllustratedBook.Services
{
    public class BookService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public BookService(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public BookViewModel? GetBook(string bookId)
        {
            var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Books", $"{bookId}.json");
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<BookViewModel>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
} 