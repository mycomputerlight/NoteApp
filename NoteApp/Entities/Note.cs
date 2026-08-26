namespace NoteApp.Entities
{
    public class Note
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string? Content { get; set; }

        public string? FileName { get; set; }

        public string? RealFileName { get; set; }

        public string? FilePath { get; set; }

        public NoteType NoteType { get; set; } = NoteType.Text;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public bool IsFavorite { get; set; }

        public bool IsArchived { get; set; } = false;

        public bool IsPinned { get; set; } = false;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }
    }

    public enum NoteType
    {
        Text = 1,
        Image = 2,
        Pdf = 3,
        Word = 4,
        PowerPoint = 5,
        TextFile = 6,
        Excel = 7,
        Video = 8,
        Audio = 9
    }
}
