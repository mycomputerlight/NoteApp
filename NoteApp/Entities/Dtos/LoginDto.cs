using System.ComponentModel.DataAnnotations;

namespace NoteApp.Entities.Dtos
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string Mail { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
