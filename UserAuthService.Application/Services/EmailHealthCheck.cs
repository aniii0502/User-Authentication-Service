using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using UserAuthService.Application.Interfaces;

namespace UserAuthService.Application.Services
{
    public class EmailHealthCheck : IHealthCheck
    {
        private readonly IEmailService _emailService;

        public EmailHealthCheck(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(1);
            // Example check: optionally send a test email or ping the service
            bool isHealthy = true;

            // TODO: Implement actual email check (e.g., SMTP ping or SendGrid test)

            return isHealthy
                ? HealthCheckResult.Healthy("Email service is OK")
                : HealthCheckResult.Unhealthy("Email service failed");
        }
    }
}
