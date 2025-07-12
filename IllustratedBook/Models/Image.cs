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
        public Book Book { get; set; } = new Book();

        /// <summary>
        /// The chapter this image belongs to
        /// </summary>
        public int ChapterId { get; set; }
        public Section Chapter { get; set; } = new Section();

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
        /// The URL where the image is stored (either local file path or external URL)
        /// </summary>
        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// The AI model that was used to generate this image
        /// </summary>
        [Required]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// The model version that was used
        /// </summary>
        public string? ModelVersion { get; set; }

        /// <summary>
        /// The width of the generated image
        /// </summary>
        public int Width { get; set; } = 1024;

        /// <summary>
        /// The height of the generated image
        /// </summary>
        public int Height { get; set; } = 1024;

        /// <summary>
        /// The number of inference steps used
        /// </summary>
        public int InferenceSteps { get; set; } = 20;

        /// <summary>
        /// The guidance scale used
        /// </summary>
        public double GuidanceScale { get; set; } = 7.5;

        /// <summary>
        /// The negative prompt used
        /// </summary>
        public string? NegativePrompt { get; set; }

        /// <summary>
        /// When this image was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this record was created in the database
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this record was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 