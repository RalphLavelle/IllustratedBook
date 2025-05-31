using Microsoft.AspNetCore.Mvc;
using System.Linq;
using IllustratedBook.Models;
using IllustratedBook.ViewModels;

namespace IllustratedBook.Controllers
{
    public class HomeController : Controller
    {
        private DataContext context;

        public HomeController(DataContext ctx)
        {
            context = ctx;
        }

        public IActionResult Index([FromQuery] string? selectedBook)
        {
            return View(new BookListViewModel {
                Books = context.Books.OrderBy(b => b.Title).ToList(),
                SelectedBook = selectedBook
            });
        }
    }
}
