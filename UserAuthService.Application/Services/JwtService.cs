using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserAuthService.Domain.Config;
using UserAuthService.Domain.Entities;
using UserAuthService.Application.Interfaces;


namespace UserAuthService.Application.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtOptions _jwtoptions;

        public JwtService(IOptions<JwtOptions> options)
        {
            _jwtoptions = options.Value;
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("username", user.Username),
        new Claim(ClaimTypes.Role, user.Role) // <-- single role
    };

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_jwtoptions.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenExpiry = DateTime.UtcNow.AddMinutes(_jwtoptions.AccessTokenExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtoptions.Issuer,
                audience: _jwtoptions.Audience,
                claims: claims,
                expires: tokenExpiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
