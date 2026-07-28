using Microsoft.EntityFrameworkCore;
using NoteApp.Auth;
using NoteApp.Entities;


namespace NoteApp.Data
{
    public class AppDbContext : DbContext       
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Note> Notes { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<RefreshToken>RefreshTokens { get; set; }
    }
}
