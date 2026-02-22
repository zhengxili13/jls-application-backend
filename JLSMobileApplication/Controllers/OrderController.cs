using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JLSApplicationBackend.ApplicationServices;
using JLSApplicationBackend.Heplers;
using JLSDataAccess.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSApplicationBackend.Controllers;

[Authorize]
[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class OrderController(
    IOrderRepository order,
    IOrderServices orderServices,
    ILogger<OrderController> logger) : Controller
{
    [HttpPost]
    public async Task<JsonResult> SaveOrder([FromBody] SaveOrderCriteria criteria)
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        
            var orderId = await orderServices.CreateOrder(criteria);
            return Json(new ApiResult
            {
                Data = orderId,
                Msg = "OK",
                Success = true,
                DataExt = userEmail
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetOrdersListByUserId(int userId, string statusCode, string lang, int? step,
        int? begin)
    {
        try
        {
            var result = await order.GetOrdersListByUserId(userId, statusCode, lang);
            if (step is > 0 && begin is >= 0)
                result = result.Skip((int)begin * (int)step).Take((int)step).ToList();

            return Json(new ApiResult
            {
                Data = result,
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetOrdersListByOrderId(long orderId, string lang)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await order.GetOrdersListByOrderId(orderId, lang),
                Msg = "OK",
                Success = true
            });
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }
}