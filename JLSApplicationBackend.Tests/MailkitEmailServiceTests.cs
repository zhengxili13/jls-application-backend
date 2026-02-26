using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace JLSApplicationBackend.Tests;

[TestFixture]
public class MailkitEmailServiceTests
{
    private IOptions<AppSettings> _appSettings;
    private Mock<ILogger<MailkitEmailService>> _mockLogger;

    [SetUp]
    public void Setup()
    {
        _appSettings = Options.Create(new AppSettings
        {
            EmailAccount = "test@example.com",
            EmailPassword = "password",
            EmailHost = "smtp.example.com",
            EmailPort = 465
        });
        _mockLogger = new Mock<ILogger<MailkitEmailService>>();
    }

    // ─────────────────── Construction Tests ───────────────────

    [Test]
    public void Constructor_ShouldNotThrow_WithValidSettings()
    {
        Assert.DoesNotThrow(() => new MailkitEmailService(_appSettings, _mockLogger.Object));
    }

    // ─────────────────── SendEmailAsync Tests ───────────────────
    // NOTE: These tests verify the behavior of the service without actually
    //       connecting to a real SMTP server. The SmtpClient in MailKit is a
    //       concrete class and cannot be mocked without abstraction, so we test
    //       that failures are handled gracefully and logged properly.

    [Test]
    public async Task SendEmailAsync_ShouldReturnErrorMessage_WhenSmtpConnectionFails()
    {
        // Arrange - point to a host that will definitely fail to connect
        var badSettings = Options.Create(new AppSettings
        {
            EmailAccount = "test@example.com",
            EmailPassword = "password",
            EmailHost = "localhost",   // no SMTP running here
            EmailPort = 19999         // unlikely port
        });
        var service = new MailkitEmailService(badSettings, _mockLogger.Object);

        // Act
        var result = await service.SendEmailAsync("recipient@test.com", "Test Subject", "<p>Hello</p>");

        // Assert - should NOT throw, just return error message
        Assert.That(result, Is.Not.EqualTo("Email Sent Successfully!"));
        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task SendEmailAsync_ShouldLogError_WhenSmtpConnectionFails()
    {
        var badSettings = Options.Create(new AppSettings
        {
            EmailAccount = "test@example.com",
            EmailPassword = "password",
            EmailHost = "localhost",
            EmailPort = 19999
        });
        var service = new MailkitEmailService(badSettings, _mockLogger.Object);

        await service.SendEmailAsync("recipient@test.com", "Test", "<p>Hi</p>");

        // Verify that LogError was called (structured logging makes this a bit involved)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_ShouldNotThrow_WithNullAttachmentPath()
    {
        var badSettings = Options.Create(new AppSettings
        {
            EmailAccount = "test@example.com",
            EmailPassword = "password",
            EmailHost = "localhost",
            EmailPort = 19999
        });
        var service = new MailkitEmailService(badSettings, _mockLogger.Object);

        // Act - should gracefully fail (connection) but NOT throw due to null attachment
        var result = await service.SendEmailAsync("recipient@test.com", "Test", "<p>Hi</p>", null);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task SendEmailAsync_ShouldNotThrow_WithEmptyAttachmentPath()
    {
        var badSettings = Options.Create(new AppSettings
        {
            EmailAccount = "test@example.com",
            EmailPassword = "password",
            EmailHost = "localhost",
            EmailPort = 19999
        });
        var service = new MailkitEmailService(badSettings, _mockLogger.Object);

        // Empty string attachment should be treated the same as null
        var result = await service.SendEmailAsync("recipient@test.com", "Test", "<p>Hi</p>", "");

        Assert.That(result, Is.Not.Null);
    }

    // ─────────────────── Interface Compliance ───────────────────

    [Test]
    public void MailkitEmailService_ShouldImplementIEmailService()
    {
        var service = new MailkitEmailService(_appSettings, _mockLogger.Object);
        Assert.That(service, Is.InstanceOf<IEmailService>());
    }
}
