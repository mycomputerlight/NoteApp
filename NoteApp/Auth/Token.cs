namespace NoteApp.Auth;

public class Token
{
    public string accessToken { get; set; } =  string.Empty;
    public DateTime expiration { get; set; }
}