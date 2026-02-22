using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSApplicationBackend.Services;
using JLSDataAccess.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSMobileApplication.Controllers;

[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class ExportController(
    IExportService exportService,
    IProductRepository productRepository,
    IOrderRepository orderRepository,
    ILogger<ExportController> logger)
    : Controller
{
    [HttpPost]
    public async Task<IActionResult> ExportAction([FromBody] ExportModel Model)
    {
        try
        {
            var list = new List<dynamic>();
            switch (Model.ExportType)
            {
                case "SimpleProductSearch":
                    var SearchText = Model.Criteria.GetValue("SearchText").Value;
                    list = await productRepository.SimpleProductSearch(SearchText, Model.Lang);
                    break;
                case "AdvancedProductSearchByCriteria":
                    var ProductLabel = Model.Criteria.GetValue("ProductLabel").Value;
                    var MainCategoryReferenceId = Model.Criteria.GetValue("MainCategoryReferenceId").Value;
                    var SecondCategoryReferenceId =
                        Model.Criteria.GetValue("SecondCategoryReferenceId").ToObject<List<long>>(); //.Value;
                    var Validity = Model.Criteria.GetValue("Validity").Value;

                    list = await productRepository.AdvancedProductSearchByCriteria(ProductLabel,
                        MainCategoryReferenceId, SecondCategoryReferenceId, Validity, Model.Lang);

                    break;

                case "AdvancedOrderSearchByCriteria":
                    DateTime? FromDate = null;
                    if (Model.Criteria.GetValue("FromDate").Value != null &&
                        Model.Criteria.GetValue("FromDate").Value != "")
                        FromDate = Convert.ToDateTime(Model.Criteria.GetValue("FromDate").Value);
                    DateTime? ToDate = null;
                    if (Model.Criteria.GetValue("ToDate").Value != null &&
                        Model.Criteria.GetValue("ToDate").Value != "")
                        ToDate = Convert.ToDateTime(Model.Criteria.GetValue("ToDate").Value);

                    var OrderId = Model.Criteria.GetValue("OrderId").Value;
                    var StatusId = Model.Criteria.GetValue("StatusId").Value;

                    var UserId = Model.Criteria.GetValue("UserId").Value;

                    list = await orderRepository.AdvancedOrderSearchByCriteria(Model.Lang, UserId, FromDate, ToDate,
                        OrderId, StatusId);
                    break;
            }

            if (list.Count() > 0)
            {
                var memory = exportService.ExportExcel(list, Model.ExportType);
                return File(memory, "application/vnd.ms-excel",
                    Model.ExportType + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx");
            }

            return NotFound();
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult> ExportPdf([FromBody] PdfExportModel model)
    {
        string fileName = await exportService.ExportPdf(model.OrderId, "Fr");

        /* Download file function */
        var fileBytes = System.IO.File.ReadAllBytes(fileName);
        return File(fileBytes, "application/x-msdownload", DateTime.Now + "_Invoice.pdf");
    }

    public class ExportModel
    {
        public string ExportType { get; set; }

        public dynamic Criteria { get; set; }

        public string Lang { get; set; }
    }

    public class PdfExportModel
    {
        public long OrderId { get; set; }
    }
}