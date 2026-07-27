using System.ComponentModel.DataAnnotations;

namespace NoteApp.Entities.Dtos
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Ad alanı zorunludur.")]
        public string Name { get; set; }


        [Required(ErrorMessage = "Soyad alanı zorunludur.")]
        public string Surname { get; set; }


        [Required(ErrorMessage = "Kullanıcı adı alanı zorunludur.")]
        public string Username { get; set; }


        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        public string Mail { get; set; }


        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Password { get; set; }

    }
}
