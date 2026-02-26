using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JLSApplicationBackend.HtmlToPdf;
using JLSDataAccess.Interfaces;
using Magicodes.ExporterAndImporter.Pdf;
using Microsoft.AspNetCore.Mvc;

namespace JLSApplicationBackend.Controllers;

/* Todo remove: migration to export controller, please teste */
[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class HtmlToPdfController : Controller
{
    private readonly IOrderRepository _orderRepository;

    public HtmlToPdfController(IOrderRepository order)
    {
        _orderRepository = order;
    }

    public async Task<ActionResult> ExportPdf()
    {
        /* File name */
        var fileName = Path.Combine("Exports", $"{DateTime.Now.Second}_Invoice.pdf");

        /* Load template from embedded resource */
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("receipt.cshtml", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("Embedded receipt.cshtml template not found.");
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        var tpl = await reader.ReadToEndAsync();

        /* GetOrderInfo todo change */
        var orderInfo = await _orderRepository.GetOrdersListByOrderId(26, "fr"); // todo change 

        /* Get order basic info */
        var receipt = new ReceiptInfo();

        var order = orderInfo.OrderInfo;
        if (order != null)
        {
            receipt.OrderId = order.Id;
            receipt.CreatedOn = order.CreatedOn ?? DateTime.Now;
            receipt.TotalPrice = (float)(order.TotalPrice ?? 0);
        }

        var customer = orderInfo.CustomerInfo;
        if (customer != null)
        {
            receipt.Username = customer.Email;
            receipt.PhoneNumber = customer.PhoneNumber;
            receipt.Entreprise = customer.EntrepriseName;
            receipt.Siret = customer.Siret;
        }

        /* Get order tax info */
        var tax = orderInfo.TaxRate;
        if (tax != null && double.TryParse(tax.Value, out var parsedTax))
            receipt.Tax = (float)(receipt.TotalPrice * parsedTax * 0.01);

        /* Get order product list info */
        var productList = orderInfo.ProductList;
        if (productList != null)
            foreach (var item in productList)
                receipt.ProductList.Add(new ReceiptProductList
                {
                    Code = item.Code,
                    Label = item.Label,
                    Price = (float)(item.Price ?? 0),
                    Quantity = item.Quantity
                });

        /* Get facturation address */
        var facturationAddress = orderInfo.ShippingAdress;
        if (facturationAddress != null) receipt.FacturationAddress = facturationAddress;
        /* Get shipping address */
        var shippingAddress = orderInfo.FacturationAdress;
        if (shippingAddress != null) receipt.ShipmentAddress = shippingAddress;

        /* Generate pdf */
        var exporter = new PdfExporter();
        var result = await exporter.ExportByTemplate(fileName, receipt
            , tpl);

        /* Download file function */
        var fileBytes = System.IO.File.ReadAllBytes(fileName);
        return File(fileBytes, "application/x-msdownload", DateTime.Now + "_Invoice.pdf");
    }
}