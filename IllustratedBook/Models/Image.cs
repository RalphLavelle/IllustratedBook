using System;
using System.ComponentModel.DataAnnotations;

namespace IllustratedBook.Models
{
    /// <summary>
    /// Represents a generated image stored in the database
    /// This allows us to reuse images without regenerating them from AI services
    /// </summary>
    public class Image
    {
        /// <summary>
        /// Unique identifier for the image
        /// </summary>
        public int ImageId { get; set; }

        /// <summary>
        /// The book this image belongs to
        /// </summary>
        public int BookId { get; set; }
        public Book? Book { get; set; }

        /// <summary>
        /// The chapter this image belongs to
        /// </summary>
        public int ChapterId { get; set; }

        /// <summary>
        /// The page number within the chapter (1-based)
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// The text prompt that was used to generate this image
        /// </summary>
        [Required]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// JSON metadata for the image (includes URL, model info, and generation settings)
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// When this record was created in the database
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // NOTE: UpdatedAt removed per Task 11
    }
} 