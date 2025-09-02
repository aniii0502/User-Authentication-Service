using System;
using System.Threading.Tasks;
using UserAuthService.Application.Interfaces;

namespace UserAuthService.Application.Services
{
    public class EmailService : IEmailService
    {
        // Simulate sending a reset link
        public Task SendResetLinkAsync(string email, string token)
        {
            Console.WriteLine($"Send reset link to {email}: {token}");
            return Task.CompletedTask;
        }

        // Simulate sending verification email
        public Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            Console.WriteLine($"Send verification email to {email}: {verificationLink}");
            return Task.CompletedTask;
        }

        // Implement SendEmailAsync as required by interface
        public Task SendEmailAsync(string to, string subject, string htmlContent, string? plainTextContent = null)
        {
            // For now, just simulate sending email
            Console.WriteLine($"Sending email to {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"HTML Content: {htmlContent}");
            if (!string.IsNullOrEmpty(plainTextContent))
                Console.WriteLine($"Plain Text: {plainTextContent}");

            return Task.CompletedTask;
        }
    }
}
