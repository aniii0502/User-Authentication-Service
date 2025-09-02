using Xunit;
using FluentAssertions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using Moq;
using UserAuthService.Infrastructure.Services;
using UserAuthService.Infrastructure.Config;

namespace UserAuthService.Tests.Services
{
    public class SendGridEmailServiceTests
    {
        [Fact]
        public async Task SendEmailAsync_Should_NotThrow_When_Response_IsSuccess()
        {
            // Arrange
            var options = new SendGridOptions
            {
                ApiKey = "fake-api-key",
                FromEmail = "noreply@test.com",
                FromName = "Test Service"
            };

            var mockClient = new Mock<ISendGridClient>();
            mockClient
                .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Response(HttpStatusCode.Accepted, null, null));

            // Pass options first, mock client second
            var service = new SendGridEmailService(options, mockClient.Object);

            // Act
            var act = async () => await service.SendEmailAsync(
                "test@example.com",
                "Hello",
                "<b>World</b>",
                "World"
            );

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendEmailAsync_Should_Throw_When_Response_Fails()
        {
            var options = new SendGridOptions
            {
                ApiKey = "fake-api-key",
                FromEmail = "noreply@test.com",
                FromName = "Test Service"
            };

            var mockClient = new Mock<ISendGridClient>();
            mockClient
                .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Response(HttpStatusCode.BadRequest, null, null));

            var service = new SendGridEmailService(options, mockClient.Object);

            // Act
            var act = async () => await service.SendEmailAsync(
                "test@example.com",
                "Fail",
                "<b>Error</b>",
                "Error"
            );

            // Assert
            await act.Should().ThrowAsync<System.Exception>()
                     .WithMessage("Failed to send email:*");
        }
    }
}
