using System;
using System.Threading.Tasks;
using UserAuthService.Application.DTOs;
using UserAuthService.Domain.Entities;

namespace UserAuthService.Application.Interfaces
{
    public interface IAuthService
    {
        Task<UserResponseDto> RegisterAsync(UserRegisterDto dto);
        Task<UserResponseDto> LoginAsync(UserLoginDto dto);
        Task<TokenResponseDto> RefreshTokenAsync(string oldRefreshToken);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string token, string newPassword);
        Task<UserResponseDto> GetCurrentUserAsync(Guid userId);
        Task<string> GenerateJwtToken(User user);
    }
}
