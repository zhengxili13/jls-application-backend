using System;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSMobileApplication.Controllers;

[Authorize]
[Route("admin/[controller]/{action}/{id?}")]
[ApiController]
public class AnalyticsController(
    IOrderRepository orderRepository,
    IAnalyticsReporsitory analyticsRepository,
    JlsDbContext context,
    ILogger<AnalyticsController> logger)
    : Controller
{
    [HttpGet]
    public async Task<JsonResult> GetAdminSalesPerformanceDashboard(string Lang)
    {
        try
        {
            var result = await analyticsRepository.GetAdminSalesPerformanceDashboard(Lang);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetVisitorAndClientInfo()
    {
        try
        {
            var result = await analyticsRepository.GetVisitorAndClientInfo();
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetRecentOrderInfo(string Lang)
    {
        try
        {
            var statusIdProgressingId = context.ReferenceItem.Where(p => p.Code == "OrderStatus_Progressing")
                .Select(p => p.Id).FirstOrDefault();
            // last 10 days 
            var progressingList =
                (await orderRepository.AdvancedOrderSearchByCriteria(Lang, null, null, null, null,
                    statusIdProgressingId)).Skip(0).Take(10); //DateTime.Now.AddDays(-10)

            var statusIdValidedId = context.ReferenceItem.Where(p => p.Code == "OrderStatus_Valid").Select(p => p.Id)
                .FirstOrDefault();
            // last 10 days 
            var validedList =
                (await orderRepository.AdvancedOrderSearchByCriteria(Lang, null, null, DateTime.Today.Date, null,
                    statusIdValidedId)).Skip(0).Take(10);

            return Json(new
            {
                ProgressingOrderList = progressingList,
                ValidedOrderList = validedList
            });
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetSalesPerformanceByYearMonth()
    {
        try
        {
            var result = await analyticsRepository.GetSalesPerformanceByYearMonth();
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetTopSaleProduct(string Lang, int? Limit)
    {
        try
        {
            var result = await analyticsRepository.GetTopSaleProduct(Lang, Limit);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetBestSalesSubCategory(string Lang, int Limit)
    {
        try
        {
            var result = await analyticsRepository.GetBestSalesSubCategory(Limit, Lang);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public JsonResult GetBestClientWidget(int Limit)
    {
        try
        {
            var result = analyticsRepository.GetBestClientWidget(Limit);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }


    [HttpGet]
    public async Task<JsonResult> GetInternalExternalSalesPerformance(string Lang)
    {
        try
        {
            var result = await analyticsRepository.GetInternalExternalSalesPerformance(Lang);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetSalesPerformanceByStatus(string Lang)
    {
        try
        {
            var result = await analyticsRepository.GetSalesPerformanceByStatus(Lang);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetTeamMemberSalesPerformance()
    {
        try
        {
            var result = await analyticsRepository.GetTeamMemberSalesPerformance();
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }
}