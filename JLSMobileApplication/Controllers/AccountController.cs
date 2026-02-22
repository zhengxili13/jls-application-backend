using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.Resources;
using JLSApplicationBackend.Services;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.Adress;
using JLSDataModel.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JLSMobileApplication.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController(
    UserManager<User> userManager,
    IAdressRepository adressRepository,
    ISendEmailAndMessageService sendEmailAndMessageService,
    IOptions<AppSettings> appSettings,
    ILogger<AccountController> logger)
    : Controller
{
    private readonly AppSettings _appSettings = appSettings.Value;

    [AllowAnonymous]
    [HttpPost("[action]")]
    public async Task<IActionResult> Register([FromBody] RegistreCriteria model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            // Step 1: 建立新地址
            model.ShipmentAdress.EntrepriseName = model.EntrepriseName;
            model.FacturationAdress.EntrepriseName = model.EntrepriseName;
            var shippingAddressId = await adressRepository.CreateOrUpdateAdress(model.ShipmentAdress);
            var facturationAddressId = await adressRepository.CreateOrUpdateAdress(model.FacturationAdress);

            // Step2: 整理用户信息(以邮箱作用户名)
            var userIdentity = new User(); // 将UserRegistrationView 映射到User(转化为User(type:User))
            userIdentity.UserName = model.Email;
            userIdentity.FacturationAdressId = facturationAddressId;
            userIdentity.PhoneNumber = model.PhoneNumber;
            userIdentity.Email = model.Email;
            userIdentity.EntrepriseName = model.EntrepriseName;
            userIdentity.Siret = model.Siret;


            var result = await userManager.CreateAsync(userIdentity, model.Password);

            // Step3: 检查注册是否成功 
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return Json(new ApiResult
                {
                    Msg = result.Errors.ToString(),
                    Success = false
                });
            }

            // Step2bis : 添加发货地址与用户的关系
            await adressRepository.CreateUserShippingAdress(shippingAddressId, userIdentity.Id);


            // Step3: 加入用户权限
            var result1 = await userManager.AddToRoleAsync(userIdentity, "Client");
            if (!result1.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                return Json(new ApiResult
                {
                    Msg = result.Errors.ToString(),
                    Success = false
                });
            }

            // Step6: 发送确认邮件
            var code = await userManager.GenerateEmailConfirmationTokenAsync(userIdentity);
            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = userIdentity.Id, code },
                HttpContext.Request.Scheme);

            await sendEmailAndMessageService.ResetPasswordOuConfirmEmailLinkAsync(userIdentity.Id, callbackUrl,
                "EmailConfirmation");
            return Json(new ApiResult
            {
                DataExt = userIdentity.Email,
                Data = 1,
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


    /// <summary>
    ///     邮箱验证生成器,并进行验证后的操作
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(code))
        {
            // TODO redirect to error page 
            ModelState.AddModelError("", "User Id and Code are required");
            return BadRequest(ModelState);
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
            // TODO redirect to error page 
            return new JsonResult("ERROR"); // cannot find the user

        await sendEmailAndMessageService.AfterResetPasswordOuConfirmEmailLinkAsync(user.Id, "AfterEmailConfirmation");
        if (user.EmailConfirmed) return Redirect(_appSettings.WebSiteUrl); // 
        var result = await userManager.ConfirmEmailAsync(user, code);

        if (result.Succeeded) return RedirectToAction("EmailConfirmed", "Notifications", new { user, code });

        /* TODO: show error plage when token is expired or other error, how error page and resent an email */
        var errors = new List<string>();
        foreach (var error in result.Errors) errors.Add(error.ToString());

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = token },
            HttpContext.Request.Scheme);

        await sendEmailAndMessageService.ResetPasswordOuConfirmEmailLinkAsync(user.Id, callbackUrl,
            "EmailConfirmation");
        return RedirectToAction("ResentEmail", "Notifications");
    }

    /// <summary>
    ///     密码重置
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    public IActionResult ResetPassword(string token)
    {
        return View();
    }


    [HttpPost("[action]")]
    public bool ResetPassword(ResetPasswordViewModel obj)
    {
        var userIdentity = userManager.FindByNameAsync(obj.UserName).Result;


        var codeDecodedBytes = WebEncoders.Base64UrlDecode(obj.Token); // decrypt
        var codeDecoded = Encoding.UTF8.GetString(codeDecodedBytes);

        var result = userManager.ResetPasswordAsync
            (userIdentity, codeDecoded, obj.Password).Result;
        if (result.Succeeded)
        {
            sendEmailAndMessageService.AfterResetPasswordOuConfirmEmailLinkAsync(userIdentity.Id, "AfterResetPassword");
            ViewBag.Message = "Password reset successful!";
            return true;
        }

        ViewBag.Message = "Error while resetting the password!";
        return false;
    }

    [HttpGet("[action]")]
    [AllowAnonymous]
    public async Task<JsonResult> SendPasswordResetLink(string username)
    {
        var user = userManager.FindByNameAsync(username).Result;

        if (user == null || !userManager.IsEmailConfirmedAsync(user).Result)
            return Json(new ApiResult
            {
                Data = "Account is not exists or not yet confirm",
                Msg = "OK",
                Success = false
            });

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        //var resetLink = Url.Action("ResetPassword",
        //    "Account", new { token = token },HttpContext.Request.Scheme);

        var tokenGeneratedBytes = Encoding.UTF8.GetBytes(token);
        var codeEncoded = WebEncoders.Base64UrlEncode(tokenGeneratedBytes); // encrypt

        var resetLink = _appSettings.WebSiteUrl + "/account/resetPassword?Token=" + codeEncoded + "&Username=" +
                        user.UserName;
        await sendEmailAndMessageService.ResetPasswordOuConfirmEmailLinkAsync(user.Id, resetLink, "ResetPassword");
        return Json(new ApiResult
        {
            DataExt = resetLink,
            Data = username,
            Msg = "OK",
            Success = true
        });
    }


    public class RegistreCriteria
    {
        public string Email { get; set; }

        public string Password { get; set; }
        public string EntrepriseName { get; set; }
        public string Siret { get; set; }
        public string PhoneNumber { get; set; }
        public Adress FacturationAdress { get; set; }

        public Adress ShipmentAdress { get; set; }
    }
}