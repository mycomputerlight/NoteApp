using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteApp.Auth;
using NoteApp.Data;
using NoteApp.Entities;
using NoteApp.Entities.Dtos;

namespace NoteApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuthController : Controller
    {
        private readonly JwtTokenHelper _jwtTokenHelper;
        private readonly IConfiguration _config;
        private readonly AppDbContext _dbContext;
        AppDbContext _context;
        public AuthController(AppDbContext context, JwtTokenHelper jwtTokenHelper, IConfiguration config, AppDbContext dbContext)
        {
            _context = context;
            _jwtTokenHelper = jwtTokenHelper;
            _config = config;
            _dbContext = dbContext;
        }

        [AllowAnonymous]
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


        [AllowAnonymous]
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
            var token = _jwtTokenHelper.CreateAccessToken(_config,user);
            var refreshToken = _jwtTokenHelper.CreateRefreshToken(_config,user);
            
            _dbContext.RefreshTokens.AddAsync(refreshToken);
            user.RefreshTokens.Add(refreshToken);
            _dbContext.Update(user);
             _dbContext.SaveChangesAsync();
            return Ok(new { token,refreshToken = refreshToken.token });
        }

        [HttpPost("refresh")]
        public IActionResult Refresh([FromBody] string refreshToken)
        {
            // is valid refresh tokenin gecerli olup olmadigini kontrol ediyor suan aklima bu isimlendirme geldi sana farkli bir isim gelirse koyarsin 
            var isValid = _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefault(rt => rt.token == refreshToken
                                      && !rt.isRevoked
                                      && !rt.isUsed
                                      && rt.expiresAt > DateTime.Now);
            if (isValid == null)
                return NotFound("Refresh token suresi dolmus veya gecersiz ");
            var newToken = _jwtTokenHelper.CreateAccessToken(_config,isValid.User);
            var newRefToken = _jwtTokenHelper.CreateRefreshToken(_config,isValid.User);
            isValid.isUsed = true;
            isValid.User.RefreshTokens.Add(newRefToken);
            
            _context.RefreshTokens.Add(newRefToken);
            _context.RefreshTokens.Update(isValid);
            _context.Users.Update(isValid.User);
            _dbContext.SaveChanges();
            return Ok(new
            {
                token=newToken,
                refreshToken=newRefToken
            });
        }
    }
}
