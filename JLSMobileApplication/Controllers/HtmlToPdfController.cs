using System;
using System.IO;
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
        var fileName = "Exports/" + DateTime.Now.Second + "_Invoice.pdf";
        /* Get template path */
        var tplPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "HtmlToPdf",
            "receipt.cshtml");
        var tpl = System.IO.File.ReadAllText(tplPath);

        /* GetOrderInfo todo change */
        var orderInfo = await _orderRepository.GetOrdersListByOrderId(26, "fr"); // todo change 

        /* Get order basic info */
        var receipt = new ReceiptInfo();

        var order = orderInfo.GetType().GetProperty("OrderInfo").GetValue(orderInfo, null);
        if (order != null)
        {
            receipt.OrderId = order.Id;
            receipt.CreatedOn = order.CreatedOn;
            receipt.TotalPrice = order.TotalPrice;
        }

        var customer = orderInfo.GetType().GetProperty("CustomerInfo").GetValue(orderInfo, null);
        if (customer != null)
        {
            receipt.Username = customer.Email;
            receipt.PhoneNumber = customer.PhoneNumber;
            receipt.Entreprise = customer.EntrepriseName;
            receipt.Siret = customer.Siret;
        }

        /* Get order tax info */
        var tax = orderInfo.GetType().GetProperty("TaxRate").GetValue(orderInfo, null);
        if (tax != null)
            receipt.Tax = receipt.TotalPrice * double.Parse(tax.GetType().GetProperty("Value").GetValue(tax, null)) *
                          0.01;

        /* Get order product list info */
        var productList = orderInfo.GetType().GetProperty("ProductList").GetValue(orderInfo, null);
        if (productList != null)
            foreach (var item in productList)
                receipt.ProductList.Add(new ReceiptProductList
                {
                    Code = item.GetType().GetProperty("Code").GetValue(item, null),
                    Label = item.GetType().GetProperty("Label").GetValue(item, null),
                    Price = item.GetType().GetProperty("Price").GetValue(item, null),
                    Quantity = item.GetType().GetProperty("Quantity").GetValue(item, null)
                });

        /* Get facturation address */
        var facturationAddress = orderInfo.GetType().GetProperty("ShippingAdress").GetValue(orderInfo, null);
        if (facturationAddress != null) receipt.FacturationAddress = facturationAddress;
        /* Get shipping address */
        var shippingAddress = orderInfo.GetType().GetProperty("FacturationAdress").GetValue(orderInfo, null);
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