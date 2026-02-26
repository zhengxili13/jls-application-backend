using System;
using System.Collections.Generic;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.HtmlToPdf;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models;
using JLSDataModel.Models.Order;
using JLSDataModel.ViewModels;
using JLSMobileApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace JLSApplicationBackend.Tests;

[TestFixture]
public class ExportService_PdfDataTests
{
    private Mock<JlsDbContext> _mockContext;
    private Mock<IOrderRepository> _mockOrderRepo;
    private Mock<IHttpContextAccessor> _mockHttpContext;
    private Mock<ILogger<ExportService>> _mockLogger;
    private IOptions<AppSettings> _appSettings;
    private ExportService _exportService;

    [SetUp]
    public void Setup()
    {
        _mockContext = new Mock<JlsDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<JlsDbContext>());
        _mockOrderRepo = new Mock<IOrderRepository>();
        _mockHttpContext = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<ExportService>>();

        _appSettings = Options.Create(new AppSettings
        {
            WebSiteUrl = "https://jls-import.com",
            ExportPath = "/tmp/exports"
        });

        _exportService = new ExportService(
            _appSettings,
            _mockContext.Object,
            _mockHttpContext.Object,
            _mockOrderRepo.Object,
            _mockLogger.Object
        );
    }

    [Test]
    public void BuildReceiptInfo_ShouldMapBasicOrderInfoCorrectly()
    {
        // Arrange
        var orderId = 12345L;
        var createdOn = new DateTime(2024, 1, 1);
        var totalPrice = 500.50f;
        
        var dto = new OrderFullDetailDto
        {
            OrderInfo = new OrderInfo
            {
                Id = orderId,
                CreatedOn = createdOn,
                TotalPrice = totalPrice
            }
        };

        // Act
        var result = _exportService.BuildReceiptInfo(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.OrderId, Is.EqualTo(orderId));
            Assert.That(result.CreatedOn, Is.EqualTo(createdOn));
            Assert.That(result.TotalPrice, Is.EqualTo(totalPrice));
        });
    }

    [Test]
    public void BuildReceiptInfo_ShouldMapCustomerInfoCorrectly()
    {
        // Arrange
        var dto = new OrderFullDetailDto
        {
            CustomerInfo = new CustomerInfo
            {
                Email = "test@user.com",
                PhoneNumber = "0612345678",
                EntrepriseName = "ACME Corp",
                Siret = "12345678901234"
            }
        };

        // Act
        var result = _exportService.BuildReceiptInfo(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Username, Is.EqualTo("test@user.com"));
            Assert.That(result.PhoneNumber, Is.EqualTo("0612345678"));
            Assert.That(result.Entreprise, Is.EqualTo("ACME Corp"));
            Assert.That(result.Siret, Is.EqualTo("12345678901234"));
        });
    }

    [Test]
    public void BuildReceiptInfo_ShouldCalculateTotalsAndTaxCorrectly()
    {
        // Arrange
        var dto = new OrderFullDetailDto
        {
            TaxRate = new TaxRateDto { Value = "20" },
            ProductList = new List<OrderProductDto>
            {
                new OrderProductDto
                {
                    Quantity = 2,
                    QuantityPerBox = 10,
                    Price = 5.50,
                    Code = "P1",
                    Label = "Product 1",
                    DefaultPhotoPath = "/path/p1.jpg"
                },
                new OrderProductDto
                {
                    Quantity = 1,
                    QuantityPerBox = 5,
                    Price = 10.00,
                    Code = "P2",
                    Label = "Product 2",
                    DefaultPhotoPath = "/path/p2.jpg"
                }
            }
        };

        // Act
        var result = _exportService.BuildReceiptInfo(dto);

        // Assert
        // TotalWithoutTax = (2 * 10 * 5.50) + (1 * 5 * 10.00) = 110 + 50 = 160
        // Tax = 160 * 20 * 0.01 = 32
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalPriceWithoutTax, Is.EqualTo(160f));
            Assert.That(result.TaxRate, Is.EqualTo(20f));
            Assert.That(result.Tax, Is.EqualTo(32f));
            Assert.That(result.ProductList, Has.Count.EqualTo(2));
            Assert.That(result.ProductList[0].PhotoPath, Is.EqualTo("https://jls-import.com/path/p1.jpg"));
        });
    }

    [Test]
    public void BuildReceiptInfo_ShouldMapAddressesCorrectly()
    {
        // Arrange
        var factAddr = new JLSDataModel.Models.Adress.Adress { City = "Paris", FirstLineAddress = "10 Rue A" };
        var shipAddr = new JLSDataModel.Models.Adress.Adress { City = "Lyon", FirstLineAddress = "5 Blvd B" };
        
        var dto = new OrderFullDetailDto
        {
            FacturationAdress = factAddr,
            ShippingAdress = shipAddr
        };

        // Act
        var result = _exportService.BuildReceiptInfo(dto);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.FacturationAddress.City, Is.EqualTo("Paris"));
            Assert.That(result.ShipmentAddress.City, Is.EqualTo("Lyon"));
        });
    }

    [Test]
    public void BuildReceiptInfo_ShouldHandleClientRemark()
    {
        // Arrange
        var dto = new OrderFullDetailDto
        {
            ClientRemark = new Remark { Text = "Handle with care" }
        };

        // Act
        var result = _exportService.BuildReceiptInfo(dto);

        // Assert
        Assert.That(result.ClientRemark, Is.EqualTo("Handle with care"));
    }
}
