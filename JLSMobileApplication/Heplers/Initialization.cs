using System;
using System.Linq;
using System.Text;
using JLSDataAccess;
using JLSDataModel.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace JLSApplicationBackend.Heplers;

public class Initialization
{
    public static AppSettings _appSettings;

    public Initialization(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public static void AddAdminUser(UserManager<User> userManager, JlsDbContext db)
    {
        var superAdminList = _appSettings.SuperAdminList.Split(';').ToList();
        ;
        foreach (var item in superAdminList)
        {
            var adminUser = db.Users.Where(p => p.UserName == item).FirstOrDefault();
            if (adminUser == null)
            {
                /* Convert here to base64 */
                var Password = _appSettings.AdminInitialPassword;
                var PasswordInByte = Encoding.Default.GetBytes(Password);
                var encryptedPassword = Convert.ToBase64String(PasswordInByte);

                var u = new User();
                u.UserName = item;
                u.Email = item;
                u.EmailConfirmed = true;

                u.Validity = true;
                var result = userManager.CreateAsync(u, encryptedPassword).Result;
                if (result.Succeeded) userManager.AddToRoleAsync(u, "SuperAdmin").Wait();
            }
        }
    }
}