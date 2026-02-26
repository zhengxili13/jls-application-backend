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
using System.Reflection;

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
        if (List != null && List.Count > 0)
        {
            /* Step1: Get export model */
            var ExportConfiguration = context.ExportConfiguration.FirstOrDefault(p => p.ExportName == ExportName);
            List<ExportModel> ExportConfigurationModel = null;
            if (!string.IsNullOrEmpty(ExportConfiguration?.ExportModel))
            {
                ExportConfigurationModel = JsonConvert.DeserializeObject<List<ExportModel>>(ExportConfiguration.ExportModel);
            }
            
            /* Step2: Calcul the targeted Column */

            // Get columns title from first object in the list
            var columns = List[0].GetType().GetProperties();
            var targetColumns = new List<string>();
            var targetCoulmnsWithOrder = new List<ExportModel>();

            foreach (var item in columns)
            {
                if (ExportConfigurationModel != null)
                {
                    var temp = ExportConfigurationModel.FirstOrDefault(p => p.Name == item.Name);
                    if (temp != null) targetCoulmnsWithOrder.Add(temp);
                }
                else
                {
                    targetColumns.Add(item.Name);
                }
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
                    var temp = ExportConfigurationModel.Where(p => p.Name == item.Name).Select(p => p.DisplayName).FirstOrDefault();
                    var cell = header.CreateCell(columnsCounter);
                    cell.CellStyle = firstTitleStyle;
                    cell.SetCellValue(temp ?? item.Name);
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
                            valueFormatted = (bool)value ? "OUI" : "NON";
                        else if (valueType.Name == "DateTime") 
                            valueFormatted = value.ToString();
                        
                        if (column.Name.Contains("Path"))
                            value = $"{httpContextAccessor.HttpContext.Request.Host}{httpContextAccessor.HttpContext.Request.PathBase}/{value}";
                        if (column.Name.Contains("Price")) 
                            value = $"{value}€(HT)";
                    }
                    else
                    {
                        value = "";
                    }

                    if (value is IList && value.GetType().IsGenericType) valueFormatted = "";

                    var cell = datarow.CreateCell(columnsCounter);
                    cell.SetCellValue(valueFormatted ?? value?.ToString() ?? "");

                    columnsCounter++;
                }

                rowIndex++;
            }

            /* Adapt the width of excel */
            for (var columnNum = 0; columnNum < targetCoulmnsWithOrder.Count; columnNum++)
            {
                var columnWidth = (int)sheet.GetColumnWidth(columnNum) / 256;
                // 5为开始修改的行数，默认为0行开始
                for (var rowNum = 0; rowNum <= sheet.LastRowNum; rowNum++)
                {
                    var currentRow = sheet.GetRow(rowNum);
                    if (currentRow?.GetCell(columnNum) != null)
                    {
                        var currentCell = currentRow.GetCell(columnNum);
                        var length = Encoding.Default.GetBytes(currentCell.ToString()).Length + 1;
                        if (columnWidth < length) columnWidth = length;
                    }
                }

                sheet.SetColumnWidth(columnNum, columnWidth * 256);
            }

            // Write workbook directly to memory stream
            using var memory = new MemoryStream();
            workbook.Write(memory, leaveOpen: true);
            memory.Position = 0;

            // Copy to a new stream to avoid disposing issues if 'leaveOpen' isn't perfectly supported in this NPOI version
            var finalStream = new MemoryStream(memory.ToArray());
            return finalStream;
        }

        return null;
    }

    public async Task<string> ExportPdf(long OrderId, string Lang)
    {
        try
        {
            /* File name */
            var fileName = Path.Combine(_appSettings.ExportPath, $"{DateTime.Now:yyyyMMdd_HHmmss}_Invoice.pdf");

            /* Load template from embedded resource */
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("receipt.cshtml", StringComparison.OrdinalIgnoreCase))
                ?? throw new FileNotFoundException("Embedded receipt.cshtml template not found.");
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var tpl = await reader.ReadToEndAsync();

            /* GetOrderInfo */
            var orderInfo = await orderRepository.GetOrdersListByOrderId(OrderId, Lang);

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

            var clientRemark = orderInfo.ClientRemark;
            if (clientRemark?.Text != null) 
                receipt.ClientRemark = clientRemark.Text;

            /* Get order tax info */
            var tax = orderInfo.TaxRate;
            if (tax != null && float.TryParse(tax.Value, out var parsedTax))
            {
                receipt.TaxRate = parsedTax;
            }

            /* Get order product list info */
            var productList = orderInfo.ProductList;
            if (productList != null)
            {
                foreach (var item in productList)
                {
                    receipt.ProductList.Add(new ReceiptProductList
                    {
                        Code = item.Code,
                        Colissage = item.QuantityPerBox ?? 0,
                        QuantityPerParcel = item.QuantityPerParcel ?? 0,
                        PhotoPath = $"{_appSettings.WebSiteUrl}{item.DefaultPhotoPath}",
                        Label = item.Label,
                        Price = (float)(item.Price ?? 0),
                        Quantity = item.Quantity,
                        IsModifiedPriceOrBox = item.IsModifiedPriceOrBox
                    });

                    receipt.TotalPriceWithoutTax += (float)((item.QuantityPerBox ?? 0) * (item.Price ?? 0) * item.Quantity);
                }

                if (tax != null && float.TryParse(tax.Value, out var taxValue))
                {
                    receipt.Tax = (float)(receipt.TotalPriceWithoutTax * taxValue * 0.01);
                }
            }

            /* Get facturation address */
            if (orderInfo.FacturationAdress != null) 
                receipt.FacturationAddress = orderInfo.FacturationAdress;
                
            /* Get shipping address */
            if (orderInfo.ShippingAdress != null) 
                receipt.ShipmentAddress = orderInfo.ShippingAdress;

            /* Generate pdf */
            var exporter = new PdfExporter();
            var result = await exporter.ExportByTemplate(fileName, receipt, tpl);

            return fileName;
        }
        catch (Exception e)
        {
            logger.LogError(e, "ExportPdf failed.");
            throw;
        }
    }
}