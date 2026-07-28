using System.Text.Json.Serialization;
using NoteApp.Entities;

namespace NoteApp.Auth;

public class RefreshToken
{
    public Guid id { get; set; } = new Guid();
    public string? token { get; set; }
    public DateTime expiresAt { get; set; } 
    public bool isUsed { get; set; } = false;
    public bool isRevoked { get; set; } = false;
    public Guid UserId { get; set; }
    [JsonIgnore]
    public User User { get; set; } = null!;
}