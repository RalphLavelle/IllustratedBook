using System.Text.Json;
using System.Text.RegularExpressions;
using IllustratedBook.ViewModels;
using IllustratedBook.Models;
using Microsoft.EntityFrameworkCore;

namespace IllustratedBook.Services
{
    public class BookService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly DataContext _dataContext;

        public BookService(IWebHostEnvironment hostingEnvironment, DataContext dataContext)
        {
            _hostingEnvironment = hostingEnvironment;
            _dataContext = dataContext;
        }

        // Get all books from the database
        public async Task<IEnumerable<Book>> GetAllBooksAsync()
        {
            return await _dataContext.Books
                .Include(b => b.Author)
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        // Get a book by ID from the database
        public async Task<Book?> GetBookByIdAsync(int bookId)
        {
            return await _dataContext.Books
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.BookId == bookId);
        }

        // Get sections (chapters) for a book
        public async Task<IEnumerable<Section>> GetBookSectionsAsync(int bookId)
        {
            return await _dataContext.Sections
                .Where(s => s.BookId == bookId)
                .OrderBy(s => s.SectionId)
                .ToListAsync();
        }

        // Get a specific section by ID
        public async Task<Section?> GetSectionByIdAsync(int sectionId)
        {
            return await _dataContext.Sections
                .Include(s => s.Book)
                .FirstOrDefaultAsync(s => s.SectionId == sectionId);
        }

        // Generate a slug from a title
        public string GenerateSlug(string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;

            // Convert to lowercase and replace spaces with hyphens
            var slug = title.ToLowerInvariant();
            
            // Remove special characters and replace with hyphens
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            
            // Replace multiple spaces or hyphens with single hyphen
            slug = Regex.Replace(slug, @"[\s-]+", "-");
            
            // Remove leading and trailing hyphens
            slug = slug.Trim('-');
            
            return slug;
        }

        // Create a new book with slug generation
        public async Task<Book> CreateBookAsync(string title, string authorName)
        {
            var book = new Book
            {
                Title = title,
                AuthorName = authorName,
                CreatedAt = DateTime.UtcNow
            };

            _dataContext.Books.Add(book);
            await _dataContext.SaveChangesAsync();
            
            return book;
        }

        // Create a new section (chapter) with slug generation
        public async Task<Section> CreateSectionAsync(int bookId, string title, string? content = null)
        {
            var section = new Section
            {
                BookId = bookId,
                Title = title,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _dataContext.Sections.Add(section);
            await _dataContext.SaveChangesAsync();
            
            return section;
        }

        // Update section content
        public async Task<bool> UpdateSectionContentAsync(int sectionId, string content)
        {
            var section = await _dataContext.Sections.FindAsync(sectionId);
            if (section != null)
            {
                section.Content = content;
                await _dataContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        // Legacy method for JSON-based books (keeping for backward compatibility)
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