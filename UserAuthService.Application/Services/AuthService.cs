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
using UserAuthService.Domain.Entities;
using UserAuthService.Application.Config;
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

        private const int MAX_FAILED_ATTEMPTS = 5;
        private const int LOCKOUT_DURATION_MINUTES = 15;

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

        // -------------------
        // Register
        // -------------------
        public async Task<User> RegisterAsync(string fullName, string email, string password)
        {
            var existing = await _userRepo.GetByEmailAsync(email);
            if (existing != null) throw new Exception("Email already exists");

            // ✅ Validate password
            ValidatePassword(password);

            var user = new User
            {
                FullName = fullName,
                Email = email,
                Role = "User",
                FailedLoginAttempts = 0,
                LockoutEnd = null
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _userRepo.AddAsync(user);
            return user;
        }

        // -------------------
        // Login
        // -------------------
        public async Task<(string AccessToken, string RefreshToken)> LoginAsync(string email, string password)
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null) throw new Exception("Invalid credentials");

            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                throw new Exception($"Account is locked until {user.LockoutEnd.Value}");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MAX_FAILED_ATTEMPTS)
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);

                await _userRepo.UpdateAsync(user);
                throw new Exception("Invalid credentials");
            }

            // Reset failed attempts after successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepo.UpdateAsync(user);

            // Generate tokens
            var accessToken = GenerateJwtToken(user);
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
                IsRevoked = false
            };
            await _refreshRepo.AddAsync(refreshToken);

            return (accessToken, refreshToken.Token);
        }

        // -------------------
        // Password validation
        // -------------------
        private void ValidatePassword(string password)
        {
            if (password.Length < 8)
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

        // -------------------
        // Refresh token
        // -------------------
        public async Task<string> RefreshTokenAsync(string oldRefreshToken)
        {
            var existing = await _refreshRepo.GetByTokenAsync(oldRefreshToken);
            if (existing == null || existing.ExpiresAt < DateTime.UtcNow || existing.IsRevoked)
                throw new Exception("Invalid or expired refresh token");

            var user = await _userRepo.GetByIdAsync(existing.UserId);
            if (user == null) throw new Exception("User not found");

            // Revoke old token
            existing.IsRevoked = true;
            await _refreshRepo.UpdateAsync(existing);

            // Create new refresh token
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays),
                IsRevoked = false
            };
            await _refreshRepo.AddAsync(refreshToken);

            return GenerateJwtToken(user);
        }

        // -------------------
        // Forgot password
        // -------------------
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

        // -------------------
        // Reset password
        // -------------------
        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            var reset = await _passwordResetRepo.GetByTokenAsync(token);
            if (reset == null || reset.ExpiresAt < DateTime.UtcNow || reset.IsUsed)
                throw new Exception("Invalid or expired token");

            var user = await _userRepo.GetByIdAsync(reset.UserId);
            if (user == null) throw new Exception("User not found");

            // ✅ Validate password before hashing
            ValidatePassword(newPassword);

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            await _userRepo.UpdateAsync(user);

            reset.IsUsed = true;
            await _passwordResetRepo.UpdateAsync(reset);
        }

        // -------------------
        // Get current user
        // -------------------
        public async Task<User> GetCurrentUserAsync(Guid userId)
        {
            return await _userRepo.GetByIdAsync(userId);
        }

        // -------------------
        // Helper: generate JWT
        // -------------------
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtOptions.Secret);

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
            return tokenHandler.WriteToken(token);
        }
    }
}
