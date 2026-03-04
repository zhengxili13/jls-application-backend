using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using JLSApplicationBackend.HtmlToPdf;
using JLSApplicationBackend.Services;
using JLSDataModel.Models.Adress;
using NUnit.Framework;
using QuestPDF.Fluent;
using System.Net.Http;
using System.Threading.Tasks;

namespace JLSApplicationBackend.Tests;

[TestFixture]
public class PdfGenerationTest
{
    [Test]
    public async Task GenerateSamplePdf_ForManualVerification()
    {
        using var httpClient = new HttpClient();
        
        async Task<byte[]> DownloadImage(string url)
        {
            try { return await httpClient.GetByteArrayAsync(url); }
            catch { return null; }
        }

        var photo1 = await DownloadImage("https://pub-a2183ebbb6d14f539b73e652691d11a6.r2.dev/images/0/160109_1.jpg");
        var photo2 = await DownloadImage("https://pub-a2183ebbb6d14f539b73e652691d11a6.r2.dev/images/0/008.jpg");

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
                    IsModifiedPriceOrBox = true,
                    PhotoData = photo1
                },
                new ReceiptProductList
                {
                    Code = "ORA-500",
                    Label = "Organic Oranges",
                    Price = 2.00f,
                    Quantity = 5,
                    Colissage = 10,
                    QuantityPerParcel = 5,
                    PhotoData = photo2
                }
            },
            FacturationAddress = new Adress { FirstLineAddress = "10 Main St", City = "Paris", ZipCode = "75001", Country = "France", ContactTelephone = "0102030405" },
            ShipmentAddress = new Adress { FirstLineAddress = "20 Industrial Way", City = "Lyon", ZipCode = "69001", Country = "France", ContactTelephone = "0607080910" }
        };

        var document = new ReceiptDocument(model, "Verification Invoice");

        // Act
        var uniqueId = Guid.NewGuid().ToString("n");
        var fileName = $@"C:\Dev\jls apps\jls-application-backend\QuestPDF_Verification_{uniqueId}.pdf";
        
        document.GeneratePdf(fileName);
        
        // Also generate an image for visual verification
        var imagePath = $@"C:\Dev\jls apps\jls-application-backend\QuestPDF_Verification_{uniqueId}.png";
        File.WriteAllBytes(imagePath, document.GenerateImages().First());

        // Assert
        Assert.That(System.IO.File.Exists(fileName), Is.True);
        Console.WriteLine($"PDF generated at: {System.IO.Path.GetFullPath(fileName)}");
    }
}
