using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MercuriusAPI.Models.Auth;
using MercuriusAPI.Models;

namespace MercuriusAPI.Services.Auth.Token
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private static readonly TimeSpan _refreshTokenLifetime = TimeSpan.FromDays(7);

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var jwtKey = jwtSettings["Key"];
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username)
            };
            if (user.Roles != null)
            {
                claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name)));
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiresInMinutes"] ?? "15"));
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(int userId)
        {
            var bytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(bytes),
                Expires = DateTime.UtcNow.Add(_refreshTokenLifetime),
                UserId = userId
            };
        }
    }
}
