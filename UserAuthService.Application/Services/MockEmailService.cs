using System;
using System.Threading.Tasks;
using UserAuthService.Application.Interfaces;

namespace UserAuthService.Application.Services
{
    public class MockEmailService : IEmailService
    {
        public async Task SendEmailAsync(string to, string subject, string htmlContent, string? plainTextContent = null)
        {
            Console.WriteLine("=== Mock Email Sent ===");
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"HTML Content: {htmlContent}");
            if (!string.IsNullOrEmpty(plainTextContent))
                Console.WriteLine($"Plain Text Content: {plainTextContent}");
            Console.WriteLine("=======================");
            await Task.CompletedTask;
        }

        public async Task SendResetLinkAsync(string email, string token)
        {
            Console.WriteLine($"=== Mock Reset Link ===");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Reset Token: {token}");
            Console.WriteLine("=======================");
            await Task.CompletedTask;
        }

        public async Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            Console.WriteLine($"=== Mock Verification Email ===");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Verification Link: {verificationLink}");
            Console.WriteLine("===============================");
            await Task.CompletedTask;
        }
    }
}
