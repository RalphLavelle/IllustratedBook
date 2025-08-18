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

                // Sections table removed: no seeding of chapters/pages in DB
            }
        }
    }
} 