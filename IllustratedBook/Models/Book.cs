using System;

namespace IllustratedBook.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string? Title { get; set; }
        public string? AuthorName { get; set; }
        public User? Author { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
