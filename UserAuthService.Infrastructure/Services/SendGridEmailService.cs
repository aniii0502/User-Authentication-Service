using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using UserAuthService.Application.Interfaces;  // IEmailService
using UserAuthService.Infrastructure.Config;   // SendGridOptions

namespace UserAuthService.Infrastructure.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly SendGridOptions _options;
        private readonly ISendGridClient _client;

        // Constructor: options first, client optional
        public SendGridEmailService(SendGridOptions options, ISendGridClient? client = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _client = client ?? new SendGridClient(_options.ApiKey);
        }

        // ---------------------------
        // Send Reset Link
        // ---------------------------
        public Task SendResetLinkAsync(string email, string token)
        {
            string subject = "Reset Your Password";
            string htmlContent = $"<p>Use this token to reset your password: <strong>{token}</strong></p>";
            string plainTextContent = $"Use this token to reset your password: {token}";
            return SendEmailAsync(email, subject, htmlContent, plainTextContent);
        }

        // ---------------------------
        // Send Verification Email
        // ---------------------------
        public Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            string subject = "Verify Your Email";
            string htmlContent = $"<p>Click <a href='{verificationLink}'>here</a> to verify your email.</p>";
            string plainTextContent = $"Click this link to verify your email: {verificationLink}";
            return SendEmailAsync(email, subject, htmlContent, plainTextContent);
        }

        // ---------------------------
        // Generic Send Email
        // ---------------------------
        public async Task SendEmailAsync(string to, string subject, string htmlContent, string? plainTextContent = null)
        {
            var from = new EmailAddress(_options.FromEmail, _options.FromName);
            var toEmail = new EmailAddress(to);

            var msg = MailHelper.CreateSingleEmail(
                from,
                toEmail,
                subject,
                plainTextContent ?? htmlContent,
                htmlContent
            );

            var response = await _client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to send email: {response.StatusCode}");
        }
    }
}
