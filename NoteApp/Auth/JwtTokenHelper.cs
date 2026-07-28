using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NoteApp.Entities;

namespace NoteApp.Auth;

public class JwtTokenHelper
{
    public Token CreateAccessToken(IConfiguration config,User user)
    {
        Token token = new();

        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt_Token:SecretKey"] ?? string.Empty));
        
        SigningCredentials credentials = new SigningCredentials(securityKey,SecurityAlgorithms.HmacSha512);
        token.expiration = DateTime.UtcNow.AddMinutes(Convert.ToInt16(config["Jwt_Token:ExpirationMins"]));  
        JwtSecurityToken securityToken = new(
            issuer: config["Jwt_Token:Issuer"],
            audience: config["Jwt_Token:Audience"],
            expires: token.expiration,
            notBefore: DateTime.UtcNow,
            signingCredentials: credentials,
            claims:
            [
                new Claim("userId",user.Id.ToString()),
                new Claim("name",user.Name),
                new Claim("surname",user.Surname),
                new Claim("mail",user.Mail)
            ]
        );
        JwtSecurityTokenHandler tokenHandler = new();
        token.accessToken = tokenHandler.WriteToken(securityToken);

        return token;

    }

    public RefreshToken CreateRefreshToken(IConfiguration config,User user)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        
        RefreshToken refreshToken = new RefreshToken
        {
            UserId = user.Id,
            User =  user,
            expiresAt = DateTime.UtcNow.AddHours(Convert.ToInt16(config["Jwt_Token:RefreshTokenExpirationHours"])),
            isRevoked = false,
            isUsed = false,
            token = Convert.ToBase64String(randomBytes),
        };
        return  refreshToken;
    }
}