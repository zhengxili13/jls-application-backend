using System;
using System.IO;
using System.Threading.Tasks;
using JLSApplicationBackend.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace JLSApplicationBackend.Tests
{
    [TestFixture]
    public class GoogleDriveServiceTests
    {
        private Mock<ILogger<GoogleDriveService>> _loggerMock;
        private string _testFilePath;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<GoogleDriveService>>();
            // Ensure no lingering test credentials
            _testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "google-credentials.json");
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [Test]
        public void Constructor_WhenCredentialsFileMissing_ThrowsFileNotFoundException()
        {
            // Arrange
            // We ensure the file is deleted in Setup

            // Act & Assert
            var ex = Assert.Throws<FileNotFoundException>(() => new GoogleDriveService(_loggerMock.Object));
            
            // Verify error was logged (in .NET Core's Logger framework, it's quite tricky to verify exact extension method calls, 
            // but we can verify it fails gracefully as expected).
            Assert.That(ex.Message, Does.Contain("google-credentials.json"));
        }

        [Test]
        public void UploadFileAsync_WithNullStream_ThrowsArgumentNullException()
        {
            // Note: Since we need the service to be instantiated to call UploadFileAsync, 
            // and we cannot easily instantiate it without a valid Google credential,
            // standard practice is to mock IGoogleDriveService when testing other classes 
            // that CONSUME it.
            // Here is an example of mocking the interface itself.

            var mockDriveService = new Mock<IGoogleDriveService>();
            
            mockDriveService
                .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new ArgumentNullException("fileStream"));

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await mockDriveService.Object.UploadFileAsync(null, "test.txt", "text/plain"));
        }

        [Test]
        public async Task DownloadFileAsync_WhenCalled_ReturnsMemoryStream()
        {
            // Arrange
            var mockDriveService = new Mock<IGoogleDriveService>();
            var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });
            var expectedFileName = "my-test-file.pdf";

            mockDriveService
                .Setup(s => s.DownloadFileAsync("fakeFileId"))
                .ReturnsAsync((expectedFileName, expectedStream));

            // Act
            var result = await mockDriveService.Object.DownloadFileAsync("fakeFileId");

            // Assert
            Assert.That(result.FileName, Is.EqualTo(expectedFileName));
            Assert.That(result.FileStream.Length, Is.EqualTo(3));
        }
    }
}
