using System;
using System.IO;
using System.Threading.Tasks;
using JLSApplicationBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace JLSApplicationBackend.Tests;

[TestFixture]
public class ImageServiceTests
{
    private Mock<ICloudflareR2Service> _mockR2Service;
    private Mock<ILogger<ImageService>> _mockLogger;
    private ImageService _imageService;

    [SetUp]
    public void Setup()
    {
        _mockR2Service = new Mock<ICloudflareR2Service>();
        _mockLogger = new Mock<ILogger<ImageService>>();
        _imageService = new ImageService(_mockR2Service.Object, _mockLogger.Object);
    }

    [Test]
    public async Task UploadProductImageAsync_ShouldUploadAndReturnCorrectDbPath()
    {
        // Arrange
        var productId = 123L;
        var mockFile = new Mock<IFormFile>();

        var content = "dummy image content";
        var fileName = "test-image.png";
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(ms.Length);
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        mockFile.Setup(f => f.ContentDisposition).Returns("form-data; name=\"file\"; filename=\"test-image.png\"");

        _mockR2Service.Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("returned-file-id"); // R2 Key

        // Act
        var dbPath = await _imageService.UploadProductImageAsync(productId, mockFile.Object);

        // Assert
        Assert.That(dbPath, Does.StartWith($"Images/{productId}/"));
        Assert.That(dbPath, Does.EndWith(fileName));
        _mockR2Service.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), "image/png", $"Images/{productId}"), Times.Once);
    }

    [Test]
    public async Task DeleteImageAsync_ShouldCallR2ServiceWithCorrectKey()
    {
        // Arrange
        var imagePath = "Images/123/20261111_test.png";

        _mockR2Service.Setup(s => s.DeleteFileAsync(imagePath, null)).Returns(Task.CompletedTask);

        // Act
        await _imageService.DeleteImageAsync(imagePath);

        // Assert
        _mockR2Service.Verify(s => s.DeleteFileAsync(imagePath, null), Times.Once);
    }
}
