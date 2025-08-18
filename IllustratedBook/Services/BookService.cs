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

        // Get chapters for a book from JSON file (no longer using Sections table)
        public async Task<IEnumerable<ChapterViewModel>> GetBookSectionsAsync(int bookId)
        {
            // Try to get the book from JSON first
            var jsonBook = GetBookFromJson(bookId);
            if (jsonBook != null)
            {
                // Return chapters directly from JSON
                var chapters = new List<ChapterViewModel>();
                for (int i = 0; i < jsonBook.Chapters.Count; i++)
                {
                    var chapter = jsonBook.Chapters[i];
                    chapters.Add(new ChapterViewModel
                    {
                        Index = i + 1,
                        Title = chapter.Title,
                        Pages = chapter.Pages
                    });
                }
                return await Task.FromResult(chapters);
            }

            // If JSON not found, return empty list (Sections table is removed)
            return await Task.FromResult(Enumerable.Empty<ChapterViewModel>());
        }

        // Get a specific chapter by index from JSON files
        public async Task<ChapterViewModel?> GetSectionByIdAsync(int sectionId)
        {
            // Search JSON files for a matching chapter index (index + 1)
            var booksDirectory = Path.Combine(_hostingEnvironment.ContentRootPath, "Books");
            if (Directory.Exists(booksDirectory))
            {
                var jsonFiles = Directory.GetFiles(booksDirectory, "*.json");
                foreach (var jsonFile in jsonFiles)
                {
                    var bookId = Path.GetFileNameWithoutExtension(jsonFile);
                    if (int.TryParse(bookId, out int id))
                    {
                        var jsonBook = GetBookFromJson(id);
                        if (jsonBook != null && sectionId >= 1 && sectionId <= jsonBook.Chapters.Count)
                        {
                            var chapter = jsonBook.Chapters[sectionId - 1];
                            return await Task.FromResult(new ChapterViewModel
                            {
                                Index = sectionId,
                                Title = chapter.Title,
                                Pages = chapter.Pages
                            });
                        }
                    }
                }
            }

            // Not found
            return null;
        }

        // Get book data from JSON file
        public BookViewModel? GetBookFromJson(int bookId)
        {
            var path = Path.Combine(_hostingEnvironment.ContentRootPath, "Books", $"{bookId}.json");
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<BookViewModel>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                // Log the error (in a real application, you'd use a proper logging framework)
                Console.WriteLine($"Error reading JSON file {path}: {ex.Message}");
                return null;
            }
        }

        // Get chapters for a book from JSON file
        public List<ChapterViewModel>? GetChaptersFromJson(int bookId)
        {
            var book = GetBookFromJson(bookId);
            return book?.Chapters;
        }

        // Get a specific chapter by index from JSON file
        public ChapterViewModel? GetChapterFromJson(int bookId, int chapterIndex)
        {
            var chapters = GetChaptersFromJson(bookId);
            if (chapters != null && chapterIndex >= 0 && chapterIndex < chapters.Count)
            {
                return chapters[chapterIndex];
            }
            return null;
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

        // Sections removed: creation and update methods deleted

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