using BCrypt.Net;
using System;
using System.Linq;
using System.Threading.Tasks;
using UserAuthService.Application.DTOs;
using UserAuthService.Application.Interfaces;
using UserAuthService.Domain.Entities;

namespace UserAuthService.Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetRepository _passwordResetRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtService _jwtService;

        public UserService(
            IUserRepository userRepository,
            IPasswordResetRepository passwordResetRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtService jwtService)
        {
            _userRepository = userRepository;
            _passwordResetRepository = passwordResetRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtService = jwtService;
        }

        // -------------------
        // User retrieval methods
        // -------------------
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _userRepository.GetByIdAsync(userId);
        }

        // -------------------
        // Register
        // -------------------
        public async Task<User> RegisterUserAsync(UserRegisterDto dto)
        {
            var existing = await _userRepository.GetByEmailAsync(dto.Email);
            if (existing != null)
                throw new Exception("Email already in use");

            ValidatePassword(dto.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _userRepository.AddAsync(user);
            return user;
        }

        public void ValidatePassword(string password)
        {
            if (password.Length < 8)
                throw new Exception("Password must be at least 8 characters");
            if (!password.Any(char.IsUpper))
                throw new Exception("Password must contain at least one uppercase letter");
            if (!password.Any(char.IsLower))
                throw new Exception("Password must contain at least one lowercase letter");
            if (!password.Any(char.IsDigit))
                throw new Exception("Password must contain at least one number");
            if (!password.Any(ch => "!@#$%^&*()_+-=[]{}|;':\",.<>/?".Contains(ch)))
                throw new Exception("Password must contain at least one special character");
        }

        // -------------------
        // Login
        // -------------------
        public async Task<(string AccessToken, string RefreshToken)> LoginAsync(UserLoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null)
                throw new Exception("Invalid credentials");

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                throw new Exception($"Account locked. Try again at {user.LockoutEnd.Value}");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);

                await _userRepository.UpdateAsync(user);
                throw new Exception("Invalid credentials");
            }

            // Reset failed attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepository.UpdateAsync(user);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            return (accessToken, refreshToken);
        }

        // -------------------
        // Refresh Token
        // -------------------
        public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string oldRefreshToken)
        {
            var tokenEntry = await _refreshTokenRepository.GetByTokenAsync(oldRefreshToken);
            if (tokenEntry == null || tokenEntry.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invalid or expired refresh token");

            var user = await _userRepository.GetByIdAsync(tokenEntry.UserId)
                       ?? throw new Exception("User not found");

            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Rotate token: invalidate old, store new
            tokenEntry.Token = newRefreshToken;
            tokenEntry.ExpiresAt = DateTime.UtcNow.AddDays(7);
            await _refreshTokenRepository.UpdateAsync(tokenEntry);

            return (newAccessToken, newRefreshToken);
        }

        // -------------------
        // Password Reset
        // -------------------
        public async Task ForgotPasswordAsync(ForgotPasswordDto dto, IEmailService emailService)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null) return;

            var resetTokenPlain = Guid.NewGuid().ToString();
            var tokenHash = BCrypt.Net.BCrypt.HashPassword(resetTokenPlain);

            var resetToken = new PasswordResetToken
            {
                TokenHash = tokenHash,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            await _passwordResetRepository.AddAsync(resetToken);
            await emailService.SendResetLinkAsync(user.Email, resetTokenPlain);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var tokenEntry = await _passwordResetRepository.GetByTokenAsync(dto.Token);
            if (tokenEntry == null || tokenEntry.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Invalid or expired token.");

            var user = await _userRepository.GetByIdAsync(tokenEntry.UserId)
                       ?? throw new Exception("User not found");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userRepository.UpdateAsync(user);

            tokenEntry.IsUsed = true;
            await _passwordResetRepository.UpdateAsync(tokenEntry);
        }

        // -------------------
        // Manual token addition
        // -------------------
        public async Task AddPasswordResetTokenAsync(PasswordResetToken token)
        {
            await _passwordResetRepository.AddAsync(token);
        }
    }
}
