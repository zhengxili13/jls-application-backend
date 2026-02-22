using System;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.Adress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSMobileApplication.Controllers;

[Authorize]
[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class AdressController(IAdressRepository adressrepository, ILogger<AdressController> logger) : Controller
{
    [HttpPost]
    public async Task<JsonResult> CreateOrUpdateAdress([FromBody] CreateOrUpdateAdressCriteria criteria)
    {
        try
        {
            var adressId = await adressrepository.CreateOrUpdateAdress(criteria.adress);
            long userAdressId = 0;
            if (criteria.type == "shippingAdress")
                userAdressId = await adressrepository.CreateUserShippingAdress(adressId, criteria.userId);
            else if (criteria.type == "facturationAdress")
                userAdressId = await adressrepository.CreateFacturationAdress(adressId, criteria.userId);
            return Json(new ApiResult
            {
                Data = new
                {
                    AdressId = adressId,
                    UserAdressId = userAdressId
                },
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
    public async Task<JsonResult> GetUserFacturationAdress(int UserId)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await adressrepository.GetUserFacturationAdress(UserId),
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
    public async Task<JsonResult> GetUserDefaultShippingAdress(int UserId)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await adressrepository.GetUserDefaultShippingAdress(UserId),
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
    public async Task<int> RemoveShippingAddress(long AddressId)
    {
        try
        {
            return await adressrepository.RemoveShippingAddress(AddressId);
        }
        catch (Exception exc)
        {
            logger.LogError(exc.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetAddressById(long AddressId)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await adressrepository.GetAddressById(AddressId),
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
    public async Task<JsonResult> GetUserShippingAdress(int UserId)
    {
        try
        {
            return Json(new ApiResult
            {
                Data = await adressrepository.GetUserShippingAdress(UserId),
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

    /*
     * Create or update address according to the type
     */
    public class CreateOrUpdateAdressCriteria
    {
        public CreateOrUpdateAdressCriteria()
        {
            adress = new Adress();
        }

        public Adress adress { get; set; }
        public int userId { get; set; }
        public string type { get; set; }
    }
}