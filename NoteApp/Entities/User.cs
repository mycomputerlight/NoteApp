using NoteApp.Auth;

namespace NoteApp.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Mail { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool Active { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<Note> Notes { get; set; } = new List<Note>();

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
