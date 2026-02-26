using System;
using System.Collections.Generic;
using JLSApplicationBackend.HtmlToPdf;
using JLSApplicationBackend.Services;
using JLSDataModel.Models.Adress;
using NUnit.Framework;
using QuestPDF.Fluent;

namespace JLSApplicationBackend.Tests;

[TestFixture]
public class PdfGenerationTest
{
    [Test]
    public void GenerateSamplePdf_ForManualVerification()
    {
        // QuestPDF License - Required for execution
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        // Arrange: Create a complex model that exercises all features (images, red colors, etc.)
        var model = new ReceiptInfo
        {
            OrderId = 99999,
            CreatedOn = DateTime.Now,
            Entreprise = "TEST CORP INTERNATIONAL",
            Username = "admin@test.com",
            PhoneNumber = "+33 1 23 45 67 89",
            Siret = "FR 123456789",
            ClientRemark = "This is a verification remark to test the border and multi-line wrapping in the new QuestPDF document layout.",
            TaxRate = 20,
            Tax = 100,
            TotalPriceWithoutTax = 500,
            TotalPrice = 600,
            ProductList = new List<ReceiptProductList>
            {
                new ReceiptProductList
                {
                    Code = "APP-001",
                    Label = "Gala Apples (Special Price)",
                    Price = 1.50f,
                    Quantity = 10,
                    Colissage = 20,
                    IsModifiedPriceOrBox = true // Should trigger RED row
                },
                new ReceiptProductList
                {
                    Code = "ORA-500",
                    Label = "Organic Oranges",
                    Price = 2.00f,
                    Quantity = 5,
                    Colissage = 10,
                    QuantityPerParcel = 5 // Should trigger RED colissage text
                }
            },
            FacturationAddress = new Adress { FirstLineAddress = "10 Main St", City = "Paris", ZipCode = "75001", Country = "France", ContactTelephone = "0102030405" },
            ShipmentAddress = new Adress { FirstLineAddress = "20 Industrial Way", City = "Lyon", ZipCode = "69001", Country = "France", ContactTelephone = "0607080910" }
        };

        var document = new ReceiptDocument(model, "Verification Invoice");

        // Act
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var fileName = System.IO.Path.Combine(desktopPath, "QuestPDF_Verification_Sample.pdf");
        
        // If desktop is not accessible or doesn't exist (e.g. in some server environments), use project root
        if (!System.IO.Directory.Exists(desktopPath))
        {
            fileName = "QuestPDF_Verification_Sample.pdf";
        }

        document.GeneratePdf(fileName);

        // Assert
        Assert.That(System.IO.File.Exists(fileName), Is.True);
        Console.WriteLine($"PDF generated at: {System.IO.Path.GetFullPath(fileName)}");
    }
}
