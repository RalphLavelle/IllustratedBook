using IllustratedBook.Services;
using IllustratedBook.Models;
using IllustratedBook.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace IllustratedBook.Pages.Books
{
    public class ChapterModel : PageModel
    {
        private readonly BookService _bookService;

        public ChapterModel(BookService bookService)
        {
            _bookService = bookService;
        }

        public Section? Section { get; set; }
        public List<List<string>>? Pages { get; set; }
        public ChapterViewModel? Chapter { get; set; }

        [FromRoute]
        public int BookId { get; set; }

        [FromRoute]
        public string? BookSlug { get; set; }
        
        [FromRoute]
        public int ChapterId { get; set; }

        [FromRoute]
        public string? ChapterSlug { get; set; }

        public async Task OnGetAsync()
        {
            // First, try to get the chapter directly from JSON
            // ChapterId should correspond to the chapter index (1-based)
            var chapterIndex = ChapterId - 1; // Convert to 0-based index
            Chapter = _bookService.GetChapterFromJson(BookId, chapterIndex);
            
            if (Chapter != null)
            {
                // Use the chapter data directly from JSON
                Pages = Chapter.Pages;
                
                // Create a Section object for compatibility with existing views
                var book = await _bookService.GetBookByIdAsync(BookId);
                Section = new Section
                {
                    SectionId = ChapterId,
                    BookId = BookId,
                    Title = Chapter.Title,
                    Content = JsonSerializer.Serialize(Chapter.Pages),
                    CreatedAt = DateTime.UtcNow,
                    Book = book
                };
                
                // Verify the slugs match
                if (book != null)
                {
                    var generatedBookSlug = _bookService.GenerateSlug(book.Title ?? "");
                    var generatedChapterSlug = _bookService.GenerateSlug(Chapter.Title ?? "");
                    
                    if (generatedBookSlug != BookSlug || generatedChapterSlug != ChapterSlug)
                    {
                        // Redirect to the correct URL if slugs don't match
                        RedirectToPage(new { 
                            bookId = BookId, 
                            bookSlug = generatedBookSlug,
                            chapterId = ChapterId, 
                            chapterSlug = generatedChapterSlug 
                        });
                        return;
                    }
                }
            }
            else
            {
                // Fallback to database method if JSON doesn't exist
                Section = await _bookService.GetSectionByIdAsync(ChapterId);
                
                // Verify the slugs match and the section belongs to the correct book
                if (Section != null)
                {
                    var generatedBookSlug = _bookService.GenerateSlug(Section.Book.Title ?? "");
                    var generatedChapterSlug = _bookService.GenerateSlug(Section.Title);
                    
                    if (Section.BookId != BookId || generatedChapterSlug != ChapterSlug)
                    {
                        // Redirect to the correct URL if slugs don't match
                        RedirectToPage(new { 
                            bookId = BookId, 
                            bookSlug = generatedBookSlug,
                            chapterId = ChapterId, 
                            chapterSlug = generatedChapterSlug 
                        });
                        return;
                    }
                    
                    // Parse the content as pages if it exists
                    if (!string.IsNullOrEmpty(Section.Content))
                    {
                        try
                        {
                            Pages = JsonSerializer.Deserialize<List<List<string>>>(Section.Content);
                        }
                        catch
                        {
                            // If parsing fails, create a single page with the content
                            Pages = new List<List<string>> { new List<string> { Section.Content } };
                        }
                    }
                    else
                    {
                        Pages = new List<List<string>>();
                    }
                }
            }
        }
    }
} 