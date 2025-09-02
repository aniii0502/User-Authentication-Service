using System.Threading.Tasks;

namespace UserAuthService.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendResetLinkAsync(string email, string token);
        Task SendVerificationEmailAsync(string email, string verificationLink);
        Task SendEmailAsync(string to, string subject, string htmlContent, string? plainTextContent = null);
    }
}
