using JLSApplicationBackend.Heplers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JLSApplicationBackend.Controllers;

public class NotificationsController : Controller
{
    private readonly AppSettings _appSettings;

    public NotificationsController(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public IActionResult EmailConfirmed(int userId, string code)
    {
        ViewData["WebSiteUrl"] = _appSettings.WebSiteUrl;
        return View();
    }

    public IActionResult ResentEmail(int userId, string code)
    {
        ViewData["WebSiteUrl"] = _appSettings.WebSiteUrl;
        return View();
    }
}