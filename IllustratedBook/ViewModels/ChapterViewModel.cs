namespace IllustratedBook.ViewModels
{
    public class ChapterViewModel
    {
        public int Index { get; set; }
        public string? Title { get; set; }
        public List<List<string>> Pages { get; set; } = new List<List<string>>();
    }
} 