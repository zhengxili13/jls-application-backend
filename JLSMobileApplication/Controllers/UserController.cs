using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JLSApplicationBackend.Controllers;

// TODO add authorize
[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class UserController(
    UserManager<User> userManager,
    IUserRepository user,
    ILogger<UserController> logger) : Controller
{
    [HttpGet]
    public async Task<bool> CheckUserIsAlreadyExistAsync(string Username)
    {
        return await user.CheckUserIsAlreadyExist(Username);
    }

    [HttpGet]
    public async Task<long> InsertSubscribeEmail(string Email)
    {
        return await user.InsertSubscribeEmail(Email);
    }

    [HttpGet]
    public async Task<List<dynamic>> GetNoReadedDialogClient(int UserId)
    {
        return await user.GetNoReadedDialogClient(UserId);
    }

    [HttpGet]
    public async Task<long> UpdateReadedDialog(int UserId)
    {
        return await user.UpdateReadedDialog(UserId);
    }


    [HttpGet]
    public async Task<List<dynamic>> GetChatDialog(int UserId)
    {
        return await user.GetChatDialog(UserId, null);
    }


    /* Auth zoom  start */
    [Authorize]
    [HttpGet]
    public async Task<JsonResult> GetUserById(int UserId)
    {
        try
        {
            return Json(await user.GetUserById(UserId));
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<JsonResult> UpdateUserInfo(UpdateUserInfoCriteria criteria)
    {
        try
        {
            var result = await user.UpdateUserInfo(criteria.UserId, criteria.EntrepriseName, criteria.Siret,
                criteria.PhoneNumber, criteria.DefaultShippingAddressId);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<int> UpdatePassword(UpdatePasswordCriteria criteria)
    {
        try
        {
            var user = userManager.FindByIdAsync(criteria.UserId.ToString()).Result;
            if (user != null && await userManager.CheckPasswordAsync(user, criteria.PreviousPassword))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, criteria.NewPassword);
                return result.Succeeded ? 1 : 0;
            }

            return 0;
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    public class UpdateUserInfoCriteria
    {
        public int UserId { get; set; }

        public string EntrepriseName { get; set; }

        public string Siret { get; set; }

        public string PhoneNumber { get; set; }

        public long? DefaultShippingAddressId { get; set; }
    }

    public class UpdatePasswordCriteria
    {
        public int UserId { get; set; }
        public string PreviousPassword { get; set; }

        public string NewPassword { get; set; }
    }
    /* Auth zoom  end */
}