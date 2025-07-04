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

        // Get sections (chapters) for a book from JSON file
        public async Task<IEnumerable<Section>> GetBookSectionsAsync(int bookId)
        {
            // Try to get the book from JSON first
            var jsonBook = GetBookFromJson(bookId);
            if (jsonBook != null)
            {
                // Convert JSON chapters to Section objects for compatibility
                var sections = new List<Section>();
                for (int i = 0; i < jsonBook.Chapters.Count; i++)
                {
                    var chapter = jsonBook.Chapters[i];
                    var section = new Section
                    {
                        SectionId = i + 1, // Use index + 1 as section ID
                        BookId = bookId,
                        Title = chapter.Title,
                        Content = JsonSerializer.Serialize(chapter.Pages), // Store pages as JSON string
                        CreatedAt = DateTime.UtcNow
                    };
                    sections.Add(section);
                }
                return sections;
            }

            // Fallback to database if JSON file doesn't exist
            return await _dataContext.Sections
                .Where(s => s.BookId == bookId)
                .OrderBy(s => s.SectionId)
                .ToListAsync();
        }

        // Get a specific section by ID from JSON file
        public async Task<Section?> GetSectionByIdAsync(int sectionId)
        {
            // First, we need to find which book this section belongs to
            // Since we're reading from JSON, we'll need to search through all JSON files
            // For now, let's assume sectionId corresponds to the chapter index + 1
            
            // Try to find the book by looking through JSON files
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
                        if (jsonBook != null && sectionId <= jsonBook.Chapters.Count)
                        {
                            var chapter = jsonBook.Chapters[sectionId - 1]; // Convert to 0-based index
                            var book = await GetBookByIdAsync(id);
                            
                            var section = new Section
                            {
                                SectionId = sectionId,
                                BookId = id,
                                Title = chapter.Title,
                                Content = JsonSerializer.Serialize(chapter.Pages),
                                CreatedAt = DateTime.UtcNow,
                                Book = book // Set the book reference
                            };
                            return section;
                        }
                    }
                }
            }

            // Fallback to database if not found in JSON
            return await _dataContext.Sections
                .Include(s => s.Book)
                .FirstOrDefaultAsync(s => s.SectionId == sectionId);
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