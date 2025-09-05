using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserAuthService.Application.Interfaces;
using UserAuthService.Application.DTOs;
using UserAuthService.Domain.Entities;
using UserAuthService.Domain.Config;
using Microsoft.Extensions.Options;

namespace UserAuthService.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IPasswordResetRepository _passwordResetRepo;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly JwtOptions _jwtOptions;
        private readonly IEmailService _emailService;

        public AuthService(
            IUserRepository userRepo,
            IRefreshTokenRepository refreshRepo,
            IPasswordResetRepository passwordResetRepo,
            IOptions<JwtOptions> jwtOptions,
            IEmailService emailService)
        {
            _userRepo = userRepo;
            _refreshRepo = refreshRepo;
            _passwordResetRepo = passwordResetRepo;
            _jwtOptions = jwtOptions.Value;
            _passwordHasher = new PasswordHasher<User>();
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<UserResponseDto> RegisterAsync(UserRegisterDto dto)
        {
            var existing = await _userRepo.GetByEmailAsync(dto.Email);
            if (existing != null) throw new Exception("Email already exists");

            ValidatePassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
            await _userRepo.AddAsync(user);

            var token =  await GenerateJwtToken(user);

            return new UserResponseDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FullName = user.FullName,
                Token = token
            };
        }

        public async Task<UserResponseDto> LoginAsync(UserLoginDto dto)
        {
            var user = await _userRepo.GetByEmailAsync(dto.Email);
            if (user == null) throw new Exception("Invalid credentials");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                throw new Exception("Invalid credentials");

            var accessToken =await GenerateJwtToken(user);
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
                IsRevoked = false
            };
            await _refreshRepo.AddAsync(refreshToken);

            return new UserResponseDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FullName = user.FullName,
                Token = accessToken
            };
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(string oldRefreshToken)
        {
            var existing = await _refreshRepo.GetByTokenAsync(oldRefreshToken);
            if (existing == null || existing.ExpiresAt < DateTime.UtcNow || existing.IsRevoked)
                throw new Exception("Invalid or expired refresh token");

            var user = await _userRepo.GetByIdAsync(existing.UserId);
            if (user == null) throw new Exception("User not found");

            existing.IsRevoked = true;
            await _refreshRepo.UpdateAsync(existing);

            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
                IsRevoked = false
            };
            await _refreshRepo.AddAsync(refreshToken);

            return new TokenResponseDto
            {
                AccessToken =await GenerateJwtToken(user),
                RefreshToken = refreshToken.Token
            };
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null) return;

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var resetToken = new PasswordResetToken
            {
                TokenHash = token,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false
            };
            await _passwordResetRepo.AddAsync(resetToken);

            var resetLink = $"https://your-frontend.com/reset-password?token={token}&email={email}";
            await _emailService.SendResetLinkAsync(email, resetLink);
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            var reset = await _passwordResetRepo.GetByTokenAsync(token);
            if (reset == null || reset.ExpiresAt < DateTime.UtcNow || reset.IsUsed)
                throw new Exception("Invalid or expired token");

            var user = await _userRepo.GetByIdAsync(reset.UserId);
            if (user == null) throw new Exception("User not found");

            ValidatePassword(newPassword);
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            await _userRepo.UpdateAsync(user);

            reset.IsUsed = true;
            await _passwordResetRepo.UpdateAsync(reset);
        }

        public async Task<UserResponseDto> GetCurrentUserAsync(Guid userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return null!;
            return new UserResponseDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FullName = user.FullName
            };
        }

        private void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                throw new Exception("Password must be at least 8 characters.");
            if (!password.Any(char.IsUpper))
                throw new Exception("Password must contain at least one uppercase letter.");
            if (!password.Any(char.IsLower))
                throw new Exception("Password must contain at least one lowercase letter.");
            if (!password.Any(char.IsDigit))
                throw new Exception("Password must contain at least one number.");
            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                throw new Exception("Password must contain at least one special character.");
        }

        public Task<string> GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtOptions.SecretKey);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("role", user.Role)
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Task.FromResult(tokenHandler.WriteToken(token));
        }

    }
}
