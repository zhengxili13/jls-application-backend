using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JLSMobileApplication.Controllers.AdminService;

[Authorize]
[Route("admin/[controller]/{action}")]
[ApiController]
public class UserController(
    IUserRepository userRepository,
    JlsDbContext jlsDbContext,
    UserManager<User> userManager,
    ILogger<UserController> logger)
    : Controller
{
    [HttpPost]
    public async Task<JsonResult> GetUserListByRole([FromBody] List<string> Roles) // todo change take other param
    {
        try
        {
            var result = await userRepository.GetUserListByRole(Roles);

            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<JsonResult> AdvancedUserSearch(AdvancedUserSearchCriteria criteria)
    {
        try
        {
            var result =
                await userRepository.AdvancedUserSearch(criteria.UserType, criteria.Validity, criteria.Username);
            var totalCount = result.Count();
            var list = result.Skip(criteria.begin * criteria.step).Take(criteria.step);
            return Json(new
            {
                UserList = list,
                TotalCount = totalCount
            });
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetUserById(int UserId)
    {
        try
        {
            var result = await userRepository.GetUserById(UserId);
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult> CreateOrUpdateUser([FromBody] CreateOrUpdateUserCriteria criteria)
    {
        try
        {
            //var result = await _userRepository.CreateOrUpdateUser(criteria.UserId, criteria.Email, criteria.Password, criteria.RoleId, criteria.Validity);
            var role = await jlsDbContext.Roles.Where(r => r.Id == criteria.RoleId).FirstOrDefaultAsync();

            User UserToCreateOrUpdate = null;
            if (criteria.UserId == 0)
            {
                UserToCreateOrUpdate = new User();
                UserToCreateOrUpdate.CreatedOn = DateTime.Now;
                UserToCreateOrUpdate.Email = criteria.Email;
                UserToCreateOrUpdate.UserName = criteria.Email;
                /* Admin user not need to verify email */
                if (role.Name == "Admin") UserToCreateOrUpdate.EmailConfirmed = true;
            }
            else
            {
                UserToCreateOrUpdate = await jlsDbContext.Users.FindAsync(criteria.UserId);
            }

            UserToCreateOrUpdate.Validity = criteria.Validity;
            UserToCreateOrUpdate.EmailConfirmed = criteria.EmailConfirmed;

            if (criteria.Validity == false)
            {
                var refreshToken = await jlsDbContext.TokenModel.Where(p => p.UserId == criteria.UserId).ToListAsync();
                jlsDbContext.TokenModel.RemoveRange(refreshToken);
                await jlsDbContext.SaveChangesAsync();
            }

            if (criteria.UserId == 0)
            {
                var result = await userManager.CreateAsync(UserToCreateOrUpdate, criteria.Password);
                if (result.Succeeded == false) return Json(result);
            }
            else
            {
                await userManager.UpdateAsync(UserToCreateOrUpdate);
                if (criteria.Password != "")
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(UserToCreateOrUpdate);
                    await userManager.ResetPasswordAsync(UserToCreateOrUpdate, token, criteria.Password);
                }
            }


            //Remove all role for user 
            var userRoleToRemove =
                await jlsDbContext.UserRoles.Where(p => p.UserId == UserToCreateOrUpdate.Id).ToListAsync();

            jlsDbContext.UserRoles.RemoveRange(userRoleToRemove);
            await jlsDbContext.SaveChangesAsync();
            await userManager.AddToRoleAsync(UserToCreateOrUpdate, role.Name);

            return Json(UserToCreateOrUpdate.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetUserRoleList()
    {
        try
        {
            var result = await userRepository.GetUserRoleList();
            return Json(result);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    /* Chat service start */
    [HttpGet]
    public async Task<List<dynamic>> GetChatedUser(int UserId)
    {
        return await userRepository.GetChatedUser(UserId);
    }

    [HttpGet]
    public async Task<List<dynamic>> GetChatDialog(int UserId, int? AdminUserId)
    {
        return await userRepository.GetChatDialog(UserId, AdminUserId);
    }

    [HttpGet]
    public async Task<long> UpdateReadedDialog(int UserId)
    {
        return await userRepository.UpdateReadedDialog(UserId);
    }

    [HttpGet]
    public async Task<List<dynamic>> GetNoReadedDialog(int UserId)
    {
        return await userRepository.GetNoReadedDialog(UserId);
    }


    public class AdvancedUserSearchCriteria
    {
        public int? UserType { get; set; }
        public bool? Validity { get; set; }
        public string Username { get; set; }
        public int begin { get; set; }
        public int step { get; set; }
    }

    public class CreateOrUpdateUserCriteria
    {
        public int? CreatedOrUpdatedBy { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }

        public string Password { get; set; }
        public bool Validity { get; set; }
        public int RoleId { get; set; }

        public bool EmailConfirmed { get; set; }
    }
    /* Chat service end */
}