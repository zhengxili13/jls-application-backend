using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.Services;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.Product;
using JLSMobileApplication.Controllers.AdminService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Moq.EntityFrameworkCore;
using NUnit.Framework;

namespace JLSApplicationBackend.Tests;

[TestFixture]
public class ProductControllerTests
{
    private Mock<IOptions<AppSettings>> _mockAppSettings;
    private Mock<IImageService> _mockImageService;
    private Mock<IProductRepository> _mockProductRepo;
    private Mock<IReferenceRepository> _mockReferenceRepo;
    private Mock<ILogger<ProductController>> _mockLogger;
    
    private ProductController _controller;
    private Mock<JlsDbContext> _mockContext;
    private List<ProductPhotoPath> _mockPhotoData;

    [SetUp]
    public void Setup()
    {
        _mockAppSettings = new Mock<IOptions<AppSettings>>();
        _mockAppSettings.Setup(a => a.Value).Returns(new AppSettings { ImagePath = "/tmp/images" });
        
        _mockImageService = new Mock<IImageService>();
        _mockProductRepo = new Mock<IProductRepository>();
        _mockReferenceRepo = new Mock<IReferenceRepository>();
        _mockLogger = new Mock<ILogger<ProductController>>();

        var dbOptions = new DbContextOptionsBuilder<JlsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _mockContext = new Mock<JlsDbContext>(dbOptions);

        _mockPhotoData = new List<ProductPhotoPath>();
        _mockContext.Setup(c => c.ProductPhotoPath).ReturnsDbSet(_mockPhotoData);
        
        // Setup FindAsync wrapper - Moq.EntityFrameworkCore handles most but FindAsync needs it explicitly when using complex conditions or we just mock the FindAsync itself.
        // Or we can just mock FindAsync specifically on the DbSet:
        _mockContext.Setup(c => c.ProductPhotoPath.FindAsync(It.IsAny<object[]>()))
            .ReturnsAsync((object[] ids) => _mockPhotoData.Find(p => p.Id == (long)ids[0]));

        _controller = new ProductController(
            _mockAppSettings.Object,
            _mockImageService.Object,
            _mockProductRepo.Object,
            _mockReferenceRepo.Object,
            _mockContext.Object,
            _mockLogger.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _controller.Dispose();
    }

    [Test]
    public async Task RemoveImageById_ShouldCallImageServiceToDeleteFromR2_And_RemoveFromDatabase()
    {
        // Arrange
        var photoInstance = new ProductPhotoPath { Id = 99, Path = "Images/1/test-img.jpg", ProductId = 1 };
        _mockPhotoData.Add(photoInstance);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(1);

        _mockImageService.Setup(i => i.DeleteImageAsync("Images/1/test-img.jpg")).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveImageById(99);

        // Assert
        Assert.That(result, Is.EqualTo(99));
        _mockImageService.Verify(i => i.DeleteImageAsync("Images/1/test-img.jpg"), Times.Once);
        _mockContext.Verify(c => c.ProductPhotoPath.Remove(photoInstance), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadPhoto_ShouldCallImageServiceToUploadToR2_And_SavePathToDatabase()
    {
        // Arrange
        var mockHttpContext = new DefaultHttpContext();
        
        var ms = new MemoryStream();
        var streamWriter = new StreamWriter(ms);
        streamWriter.Write("test image data");
        streamWriter.Flush();
        ms.Position = 0;

        var formFile = new Mock<IFormFile>();
        formFile.Setup(f => f.Length).Returns(ms.Length);
        formFile.Setup(f => f.FileName).Returns("test.jpg");
        formFile.Setup(f => f.ContentDisposition).Returns("form-data; name=\"file\"; filename=\"test.jpg\"");
        formFile.Setup(f => f.OpenReadStream()).Returns(ms);

        var formFiles = new FormFileCollection { formFile.Object };
        var formValues = new Dictionary<string, StringValues>
        {
            { "ProductId", new StringValues("100") }
        };

        mockHttpContext.Request.Form = new FormCollection(formValues, formFiles);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = mockHttpContext
        };

        var expectedPath = "Images/100/uploaded-test.jpg";

        _mockImageService.Setup(i => i.UploadProductImageAsync(100, It.IsAny<IFormFile>()))
            .ReturnsAsync(expectedPath);

        _mockProductRepo.Setup(r => r.SavePhotoPath(100, expectedPath))
            .ReturnsAsync(101L); // Assuming it returns the new Photo ID

        // Act
        var result = await _controller.UploadPhoto();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);

        // Verify the interactions
        _mockImageService.Verify(i => i.UploadProductImageAsync(100, It.IsAny<IFormFile>()), Times.Once);
        _mockProductRepo.Verify(r => r.SavePhotoPath(100, expectedPath), Times.Once);
    }
}
