using System;

namespace IllustratedBook.Models
{
    public class Section
    {
        public int SectionId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public Book Book { get; set; }
        public int BookId { get; set; }
        public int ParentId { get; set; }
        public string? Content { get; set; }
    }
}