using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.HtmlToPdf;
using JLSApplicationBackend.Services;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using Magicodes.ExporterAndImporter.Pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace JLSMobileApplication.Services;

public class ExportService(
    IOptions<AppSettings> appSettings,
    JlsDbContext context,
    IHttpContextAccessor httpContextAccessor,
    IOrderRepository orderRepository,
    ILogger<ExportService> logger)
    : IExportService
{
    private readonly AppSettings _appSettings = appSettings.Value;

    public MemoryStream ExportExcel(List<dynamic> List, string ExportName)
    {
        if (List != null && List.Count() > 0)
        {
            /*Step0: Create file */
            var newFile = _appSettings.ExportPath + "/" + ExportName + ".xls";
            if (File.Exists(newFile)) File.Delete(newFile);

            using (var fs = new FileStream(newFile, FileMode.Create, FileAccess.Write))
            {
                /* Step1: Get export model */
                var ExportConfiguration =
                    context.ExportConfiguration.Where(p => p.ExportName == ExportName).FirstOrDefault();
                List<ExportModel> ExportConfigurationModel = null;
                if (ExportConfiguration != null && ExportConfiguration.ExportModel != null &&
                    ExportConfiguration.ExportModel != "")
                    ExportConfigurationModel =
                        JsonConvert.DeserializeObject<List<ExportModel>>(ExportConfiguration.ExportModel);
                /* Step2: Calcul the targeted Column */

                // Get columns title from first object in the list
                var columns = List[0].GetType().GetProperties();
                var targetColumns = new List<string>();
                var targetCoulmnsWithOrder = new List<ExportModel>();

                foreach (var item in columns)
                    if (ExportConfigurationModel != null)
                    {
                        var temp = ExportConfigurationModel.Where(p => p.Name == item.Name).FirstOrDefault();
                        if (temp != null) targetCoulmnsWithOrder.Add(temp);
                    }
                    else
                    {
                        targetColumns.Add(item.Name);
                    }

                targetCoulmnsWithOrder = targetCoulmnsWithOrder.OrderBy(x => x.Order).ToList();

                /*Step3: Create Excel flow */
                IWorkbook workbook = new XSSFWorkbook();
                var sheet = workbook.CreateSheet(ExportName);
                var header = sheet.CreateRow(0);

                /* Bold the title */
                var headerFont = (XSSFFont)workbook.CreateFont();
                headerFont.IsBold = true;
                var firstTitleStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                firstTitleStyle.SetFont(headerFont);

                /*Step4: Add headers*/
                var columnsCounter = 0;
                foreach (var item in targetCoulmnsWithOrder)
                {
                    if (ExportConfigurationModel != null)
                    {
                        var temp = ExportConfigurationModel.Where(p => p.Name == item.Name).Select(p => p.DisplayName)
                            .FirstOrDefault();
                        var cell = header.CreateCell(columnsCounter);
                        cell.CellStyle = firstTitleStyle;
                        cell.SetCellValue(temp != null ? temp : item.Name);
                    }
                    else
                    {
                        header.CreateCell(columnsCounter).SetCellValue(item.Name);
                    }

                    columnsCounter++;
                }

                /*Step5: Add body */
                var rowIndex = 1;
                foreach (var item in List)
                {
                    var datarow = sheet.CreateRow(rowIndex);

                    columnsCounter = 0;
                    foreach (var column in targetCoulmnsWithOrder)
                    {
                        string valueFormatted = null;
                        var value = item.GetType().GetProperty(column.Name).GetValue(item, null);
                        if (value != null)
                        {
                            var valueType = value.GetType();

                            if (valueType.Name == "Boolean")
                                valueFormatted = value ? "OUI" : "NON";
                            else if (valueType.Name == "DateTime") valueFormatted = value.ToString();
                            if (column.Name.Contains("Path"))
                                value = httpContextAccessor.HttpContext.Request.Host +
                                        httpContextAccessor.HttpContext.Request.PathBase + "/" + value;
                            if (column.Name.Contains("Price")) value = value + "€(HT)";
                        }
                        else
                        {
                            value = "";
                        }

                        if (value is IList && value.GetType().IsGenericType) valueFormatted = "";

                        var cell = datarow.CreateCell(columnsCounter);

                        cell.SetCellValue(valueFormatted != null ? valueFormatted : value);

                        columnsCounter++;
                    }

                    rowIndex++;
                }

                /* Adapt the width of excel */
                for (var columnNum = 0; columnNum < targetCoulmnsWithOrder.Count(); columnNum++)
                {
                    var columnWidth = (int)sheet.GetColumnWidth(columnNum) / 256;
                    //5为开始修改的行数，默认为0行开始
                    for (var rowNum = 0; rowNum <= sheet.LastRowNum; rowNum++)
                    {
                        var currentRow = sheet.GetRow(rowNum);
                        if (currentRow.GetCell(columnNum) != null)
                        {
                            var currentCell = currentRow.GetCell(columnNum);
                            var length = Encoding.Default.GetBytes(currentCell.ToString()).Length + 1;
                            if (columnWidth < length) columnWidth = length;
                        }
                    }

                    sheet.SetColumnWidth(columnNum, columnWidth * 256);
                }


                workbook.Write(fs);
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(newFile, FileMode.Open))
            {
                stream.CopyTo(memory);
            }

            memory.Position = 0;

            return memory;
        }

        return null;
    }

    public async Task<string> ExportPdf(long OrderId, string Lang)
    {
        try
        {
            /* File name */
            var fileName = _appSettings.ExportPath + "/" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_Invoice.pdf";
            /* Get template path */
            var tplPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "HtmlToPdf",
                "receipt.cshtml");
            var tpl = File.ReadAllText(tplPath);

            /* GetOrderInfo todo change */
            var orderInfo = await orderRepository.GetOrdersListByOrderId(OrderId, Lang); // todo change 

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

            var clientRemark = orderInfo.GetType().GetProperty("ClientRemark").GetValue(orderInfo, null);
            if (clientRemark != null && clientRemark.Text != null) receipt.ClientRemark = clientRemark.Text;
            /* Get order tax info */
            var tax = orderInfo.GetType().GetProperty("TaxRate").GetValue(orderInfo, null);

            receipt.TaxRate = float.Parse(tax.GetType().GetProperty("Value").GetValue(tax, null));

            /* Get order product list info */
            var productList = orderInfo.GetType().GetProperty("ProductList").GetValue(orderInfo, null);
            if (productList != null)
            {
                foreach (var item in productList)
                {
                    receipt.ProductList.Add(new ReceiptProductList
                    {
                        Code = item.GetType().GetProperty("Code").GetValue(item, null),
                        Colissage = item.GetType().GetProperty("QuantityPerBox").GetValue(item, null),
                        QuantityPerParcel = item.GetType().GetProperty("QuantityPerParcel").GetValue(item, null),
                        PhotoPath = _appSettings.WebSiteUrl +
                                    item.GetType().GetProperty("DefaultPhotoPath").GetValue(item, null),
                        Label = item.GetType().GetProperty("Label").GetValue(item, null),
                        Price = (float)item.GetType().GetProperty("Price").GetValue(item, null),
                        Quantity = item.GetType().GetProperty("Quantity").GetValue(item, null),
                        IsModifiedPriceOrBox = item.GetType().GetProperty("IsModifiedPriceOrBox").GetValue(item, null)
                    });

                    receipt.TotalPriceWithoutTax = (float)(receipt.TotalPriceWithoutTax +
                                                           item.GetType().GetProperty("QuantityPerBox")
                                                               .GetValue(item, null) *
                                                           item.GetType().GetProperty("Price").GetValue(item, null) *
                                                           item.GetType().GetProperty("Quantity").GetValue(item, null));
                }

                if (tax != null)
                    receipt.Tax = (float)(receipt.TotalPriceWithoutTax *
                                          float.Parse(tax.GetType().GetProperty("Value").GetValue(tax, null)) * 0.01);
            }

            /* Get facturation address */
            var facturationAddress = orderInfo.GetType().GetProperty("FacturationAdress").GetValue(orderInfo, null);
            if (facturationAddress != null) receipt.FacturationAddress = facturationAddress;
            /* Get shipping address */
            var shippingAddress = orderInfo.GetType().GetProperty("ShippingAdress").GetValue(orderInfo, null);
            if (shippingAddress != null) receipt.ShipmentAddress = shippingAddress;

            /* Generate pdf */
            var exporter = new PdfExporter();
            var result = await exporter.ExportByTemplate(fileName, receipt
                , tpl);

            return fileName;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }
}