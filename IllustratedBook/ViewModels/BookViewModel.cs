namespace IllustratedBook.ViewModels
{
    public class BookViewModel
    {
        public string? Title { get; set; }
        public DateTime Published { get; set; }
        public List<ChapterViewModel> Chapters { get; set; } = new List<ChapterViewModel>();
    }
} 