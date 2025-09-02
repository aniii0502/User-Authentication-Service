using Xunit;
using Moq;
using FluentAssertions;
using UserAuthService.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace UserAuthService.Tests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IEmailService> _emailServiceMock;

        public EmailServiceTests()
        {
            _emailServiceMock = new Mock<IEmailService>();
        }

        [Fact]
        public async Task SendEmailAsync_Should_Invoke_EmailService()
        {
            // Arrange
            _emailServiceMock
                .Setup(s => s.SendEmailAsync("test@test.com", "Subject", "Body", null))
                .Returns(Task.CompletedTask);

            // Act
            await _emailServiceMock.Object.SendEmailAsync("test@test.com", "Subject", "Body");

            // Assert
            _emailServiceMock.Verify(
                s => s.SendEmailAsync("test@test.com", "Subject", "Body", null),
                Times.Once
            );
        }

        [Fact]
        public async Task SendEmailAsync_Should_Throw_Exception_When_SendGrid_Fails()
        {
            // Arrange
            _emailServiceMock
                .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ThrowsAsync(new Exception("SendGrid error"));

            // Act
            Func<Task> act = async () =>
                await _emailServiceMock.Object.SendEmailAsync("x@y.com", "Subj", "Body");

            // Assert
            await act.Should().ThrowAsync<Exception>()
                     .WithMessage("SendGrid error");
        }
    }
}
