using System;
using System.Threading.Tasks;
using UserAuthService.Domain.Entities;

namespace UserAuthService.Application.Interfaces
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(string fullName, string email, string password);
        Task<(string AccessToken,string RefreshToken)> LoginAsync(string email, string password);
        Task<string> RefreshTokenAsync(string oldRefreshToken);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string token, string newPassword);
        Task<User> GetCurrentUserAsync(Guid userId);
    }
}
