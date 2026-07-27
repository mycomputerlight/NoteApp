namespace NoteApp.Entities
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Note> Notes { get; set; }
            = new List<Note>();
    }
}
