using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace IllustratedBook.Models
{
    public static class SeedData
    {
        public static void Initialise(DataContext context)
        {
            context.Database.Migrate();
            {
                // Check if data already exists
                if (context.Books.Any())
                {
                    return;   // DB has been seeded
                }

                // Create User
                var user = new User
                {
                    Name = "Admin User",
                    Email = "admin@example.com",
                    Username = "admin",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsAdmin = "true" // Assuming IsAdmin is a string, adjust if it's bool
                };
                context.Users.Add(user);
                context.SaveChanges(); // Save user to get UserId

                // Create Book
                var book = new Book
                {
                    Title = "My First Illustrated Book",
                    AuthorName = user.Name, // Or user.Username
                    Author = user,
                    CreatedAt = DateTime.UtcNow
                };
                context.Books.Add(book);
                context.SaveChanges(); // Save book to get BookId

                // Create Chapter (Section)
                var chapter = new Section
                {
                    Title = "Chapter 1: The Beginning",
                    CreatedAt = DateTime.UtcNow,
                    Book = book,
                    BookId = book.BookId,
                    // ParentId will be 0 or null by default if it's a top-level section
                };
                context.Sections.Add(chapter);
                context.SaveChanges(); // Save chapter to get SectionId

                // Create Pages (Sections as children of the chapter)
                var page1 = new Section
                {
                    Title = "Page 1",
                    CreatedAt = DateTime.UtcNow,
                    Book = book,
                    BookId = book.BookId,
                    ParentId = chapter.SectionId 
                };
                context.Sections.Add(page1);

                var page2 = new Section
                {
                    Title = "Page 2",
                    CreatedAt = DateTime.UtcNow,
                    Book = book,
                    BookId = book.BookId,
                    ParentId = chapter.SectionId
                };
                context.Sections.Add(page2);

                var page3 = new Section
                {
                    Title = "Page 3",
                    CreatedAt = DateTime.UtcNow,
                    Book = book,
                    BookId = book.BookId,
                    ParentId = chapter.SectionId
                };
                context.Sections.Add(page3);

                context.SaveChanges();
            }
        }
    }
} 