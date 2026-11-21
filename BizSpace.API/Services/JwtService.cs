using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BizSpace.Dtos;
using BizSpace.Models;
using Microsoft.IdentityModel.Tokens;

namespace BizSpace.Repositories.Services;

public sealed class JwtService(IConfiguration configuration) : IJwtService
{
    public AuthResponse CreateToken(UserAccount user)
    {
        var expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:ExpirationMinutes", 60));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Role = user.Role,
            ExpiresAtUtc = expires
        };
    }
}
