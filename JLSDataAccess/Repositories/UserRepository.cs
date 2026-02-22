using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models;
using JLSDataModel.Models.Adress;
using JLSDataModel.Models.User;
using JLSDataModel.Models.Website;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JLSDataAccess.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<User> _userManager;
    private readonly JlsDbContext db;

    public UserRepository(JlsDbContext jlsDbContext, UserManager<User> userManager)
    {
        db = jlsDbContext;
        _userManager = userManager;
    }

    public async Task<long> UpdateReadedDialog(int UserId)
    {
        var dialogs = await db.Dialog.Where(p => p.FromUserId == UserId).ToListAsync();

        foreach (var item in dialogs)
        {
            item.IsReaded = true;
            db.Update(item);
        }

        await db.SaveChangesAsync();

        return 1;
    }

    public async Task<long> InsertDialog(string Message, int FromUserId, int? ToUserId)
    {
        var dialog = new Dialog();

        dialog.Message = Message;
        dialog.FromUserId = FromUserId;
        dialog.ToUserId = ToUserId;

        await db.AddAsync(dialog);
        await db.SaveChangesAsync();

        return dialog.Id;
    }

    public async Task<List<dynamic>> GetNoReadedDialog(int UserId)
    {
        var result = await (from d in db.Dialog
            where d.IsReaded == false && d.FromUserId != UserId
            group d by d.FromUserId
            into g
            select new
            {
                UserId = g.Key,
                NumberOfNoReadMessage = g.Count()
            }).ToListAsync<dynamic>();
        return result.Distinct().ToList();
    }

    public async Task<List<dynamic>> GetNoReadedDialogClient(int UserId)
    {
        var result = await (from d in db.Dialog
            where d.IsReaded == false && d.ToUserId == UserId
            group d by d.FromUserId
            into g
            select new
            {
                UserId = g.Key,
                NumberOfNoReadMessage = g.Count()
            }).ToListAsync<dynamic>();
        return result.Distinct().ToList();
    }

    public async Task<List<dynamic>> GetChatDialog(int UserId, int? AdminUserId)
    {
        var result = await (from d in db.Dialog
            where d.FromUserId == UserId ||
                  (d.ToUserId == UserId && (AdminUserId == null || d.FromUserId == AdminUserId))
            orderby d.CreatedOn
            select new
            {
                MessageId = d.Id,
                Body = d.Message,
                d.CreatedOn,
                d.FromUserId,
                FromUserName = (from u in db.Users
                    where u.Id == d.FromUserId
                    select u.Email).FirstOrDefault()
            }).ToListAsync<dynamic>();
        return result.Distinct().ToList();
    }

    public async Task<List<dynamic>> GetChatedUser(int UserId)
    {
        var result = await (from u in db.Users
            join d in db.Dialog on u.Id equals d.FromUserId
            where u.Id != UserId
            orderby d.CreatedOn descending
            select u.Id).ToListAsync();
        result = result.Distinct().ToList();
        var result1 = (from r in result
            join u in db.Users on r equals u.Id
            select new
            {
                UserId = r,
                Username = u.UserName,
                LastMessage = (from d in db.Dialog
                    where d.FromUserId == r
                    orderby d.CreatedOn descending
                    select new
                    {
                        d.CreatedOn,
                        Body = d.Message,
                        d.IsReaded
                    }).FirstOrDefault()
            }).ToList<dynamic>();
        return result1;
    }

    public async Task<long> InsertSubscribeEmail(string Email)
    {
        var result = db.SubscribeEmail.Where(p => p.Email == Email).FirstOrDefault();
        if (result == null)
        {
            var EmailToInsert = new SubscribeEmail();
            EmailToInsert.Email = Email;
            db.Add(EmailToInsert);
            await db.SaveChangesAsync();
            return EmailToInsert.Id;
        }

        return 0;
    }

    public async Task<Adress> GetUserFacturationAdress(int userId)
    {
        var result = from a in db.Adress
            join u in db.Users on a.Id equals u.FacturationAdressId
            where u.Id == userId
            select a;

        return await result.FirstOrDefaultAsync();
    }

    public async Task<List<Adress>> GetUserShippingAdress(int userId)
    {
        var result = from a in db.Adress
            join ua in db.UserShippingAdress on a.Id equals ua.ShippingAdressId
            join u in db.Users on ua.UserId equals u.Id
            where ua.UserId == userId
            select a;
        return await result.ToListAsync();
    }

    public async Task<List<User>> GetUserListByRole(List<string> Roles)
    {
        var result = await (from u in db.Users
            join userRole in db.UserRoles on u.Id equals userRole.UserId
            join role in db.Roles on userRole.RoleId equals role.Id
            select u).ToListAsync();
        return result;
    }

    public async Task<List<dynamic>> AdvancedUserSearch(int? UserType, bool? Validity, string Username)
    {
        var result = from u in db.Users
            join ur in db.UserRoles on u.Id equals ur.UserId
            join r in db.Roles on ur.RoleId equals r.Id
            where (UserType == null || ur.RoleId == UserType)
                  && (Validity == null || u.Validity == Validity)
            orderby u.CreatedOn descending
            select new
            {
                u.Id,
                Username = u.UserName,
                u.EntrepriseName,
                u.Validity,
                Telephone = u.PhoneNumber,
                UserRoleId = r.Id,
                UserRoleName = r.Name,
                u.CreatedOn,
                u.UpdatedOn,
                u.EmailConfirmed,
                ZipCode = (from a in db.Adress
                    join ua in db.UserShippingAdress on a.Id equals ua.ShippingAdressId
                    orderby a.IsDefaultAdress
                    where ua.UserId == u.Id
                    select a.ZipCode).FirstOrDefault()
            }; //.Distinct().ToListAsync<dynamic>();

        if (Username != null && Username != "")
            // The field Username can search the information according to email/telephone/ entrepriseName and zipcode
            result = result.Where(p =>
                p.Username.Contains(Username) || p.EntrepriseName.Contains(Username) ||
                p.Telephone.Contains(Username) || p.ZipCode.Contains(Username));

        var finalResult = await result.ToListAsync<dynamic>();
        return finalResult.Distinct().ToList();
    }

    public async Task<List<dynamic>> GetUserRoleList()
    {
        var result = await (from r in db.Roles
            select new
            {
                r.Id, r.Name
            }).ToListAsync<dynamic>();
        return result;
    }

    public async Task<dynamic> GetUserById(int UserId)
    {
        var result = await (from u in db.Users
            join ur in db.UserRoles on u.Id equals ur.UserId
            join r in db.Roles on ur.RoleId equals r.Id
            where u.Id == UserId
            select new
            {
                u.Id,
                u.CreatedOn,
                OrderCount = db.OrderInfo.Where(p => p.UserId == UserId).Count(),
                CommentCount = db.ProductComment.Where(p => p.UserId == UserId).Count(),
                FavoriteCount = db.ProductFavorite.Where(p => p.UserId == UserId).Count(),
                RoleId = r.Id,
                u.Email,
                u.Validity,
                u.EntrepriseName,
                u.Siret,
                u.PhoneNumber,
                u.EmailConfirmed,
                ShippingAdress = (from a in db.Adress
                    join ua in db.UserShippingAdress on a.Id equals ua.ShippingAdressId
                    where ua.UserId == UserId
                    orderby a.IsDefaultAdress == true
                    select a).ToList(),
                FacturationAdress = (from a in db.Adress
                    join u in db.Users on a.Id equals u.FacturationAdressId
                    where u.Id == UserId
                    select a).FirstOrDefault()
            }).FirstOrDefaultAsync();
        return result;
    }


    public async Task<dynamic> CreateOrUpdateUser(int UserId, string Email, string Password, int RoleId, bool Validity,
        bool EmailConfirmed)
    {
        //UserRole by RoleId
        var role = await db.Roles.Where(r => r.Id == RoleId).FirstOrDefaultAsync();

        User UserToCreateOrUpdate = null;
        if (UserId == 0)
        {
            UserToCreateOrUpdate = new User();
            UserToCreateOrUpdate.CreatedOn = DateTime.Now;
            UserToCreateOrUpdate.Email = Email;
            UserToCreateOrUpdate.UserName = Email;
            if (role.Name == "Admin") UserToCreateOrUpdate.EmailConfirmed = true;
        }
        else
        {
            UserToCreateOrUpdate = await db.Users.FindAsync(UserId);
        }

        UserToCreateOrUpdate.Validity = Validity;
        UserToCreateOrUpdate.EmailConfirmed = EmailConfirmed;
        if (UserId == 0)
        {
            var result = await _userManager.CreateAsync(UserToCreateOrUpdate, Password);
            if (result.Succeeded == false) return result;
        }
        else
        {
            await _userManager.UpdateAsync(UserToCreateOrUpdate);
            if (Password != "")
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(UserToCreateOrUpdate);
                await _userManager.ResetPasswordAsync(UserToCreateOrUpdate, token, Password);
            }
        }


        //Remove all role for user 
        var userRoleToRemove = await db.UserRoles.Where(p => p.UserId == UserToCreateOrUpdate.Id).ToListAsync();

        db.UserRoles.RemoveRange(userRoleToRemove);
        await db.SaveChangesAsync();
        await _userManager.AddToRoleAsync(UserToCreateOrUpdate, role.Name);

        return UserToCreateOrUpdate.Id;
    }


    public async Task<long> UpdateUserInfo(int UserId, string EntrepriseName, string Siret, string PhoneNumber,
        long? DefaultShippingAddressId)
    {
        var user = db.Users.Find(UserId);
        if (user != null)
        {
            user.EntrepriseName = EntrepriseName;
            user.Siret = Siret;
            user.PhoneNumber = PhoneNumber;

            db.Update(user);

            await db.SaveChangesAsync();
            if (DefaultShippingAddressId != null)
            {
                var previousDefaultShippingAddress = await (from ua in db.UserShippingAdress
                    join a in db.Adress on ua.ShippingAdressId equals a.Id
                    where ua.UserId == user.Id && a.IsDefaultAdress == true
                    select a).ToListAsync();
                if (previousDefaultShippingAddress.Count() > 0)
                    foreach (var item in previousDefaultShippingAddress)
                        item.IsDefaultAdress = false;

                var defaultShippingAddress = db.Adress.Where(p => p.Id == DefaultShippingAddressId).FirstOrDefault();
                defaultShippingAddress.IsDefaultAdress = true;

                db.Update(defaultShippingAddress);
                db.SaveChanges();
            }

            return user.Id;
        }

        return 0;
    }

    public async Task<bool> CheckUserIsAlreadyExist(string Username)
    {
        var result = await db.Users.Where(p => p.UserName == Username).FirstOrDefaultAsync();
        return result != null ? true : false;
    }
}