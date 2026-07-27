using Microsoft.AspNetCore.Mvc;
using NoteApp.Data;
using NoteApp.Entities;
using NoteApp.Entities.Dtos;

namespace NoteApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        AppDbContext _context;
        public AuthController(AppDbContext context)
        {
             _context = context;
        }

        [HttpPost("register")]
        public IActionResult CreateAccount([FromBody] RegisterDto request)
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                Name = request.Name,
                Surname = request.Surname,
                Username = request.Username,
                Mail = request.Mail,
                Password = hashedPassword
            };

           _context.Users.Add(user);
            _context.SaveChanges();

            return Ok("Hesap başarıyla oluşturuldu.");
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto request)
        {
            var user=_context.Users.FirstOrDefault(u => (u.Username == request.Username ||  u.Mail == request.Mail));
            
            if (user == null)
            {
                return(BadRequest("Kullanıcı adı veya şifre hatalı."));
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(
                request.Password, 
                user.Password
            );

            if (!isPasswordValid)
            {
                return BadRequest("Kullanıcı adı veya şifre hatalı.");
            }

            return Ok(new { Message="Giriş başarılı." }); //token üretilecek
        }

    }
}
