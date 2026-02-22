using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.Resources;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JLSApplicationBackend.Auth;

[Route("api/[controller]/{action}/{id?}")]
//[ApiController]
public class AuthController : Controller
{
    private readonly IOptions<AppSettings> _appSettings;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly JlsDbContext db;

    public AuthController(IUserRepository userRepository, UserManager<User> userManager, IMapper mapper,
        JlsDbContext dbContext, IOptions<AppSettings> appSettings)
    {
        _userManager = userManager;
        db = dbContext;
        _appSettings = appSettings;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<JsonResult> Login([FromBody] LoginViewModel model)
    {
        if (model == null)
            return Json(new ApiResult
            {
                Msg = "FAIL",
                Success = false
            });

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (await _userManager.CheckPasswordAsync(user, model.Password))
        {
            if (user.EmailConfirmed == false)
                return Json(new ApiResult
                {
                    Msg =
                        "Your Email is not yet confirmed, please confirm your email and login again", // todo: 转变成code以获取翻译
                    Success = false
                });
            if (user.Validity == null || user.Validity == false)
                return Json(new ApiResult
                {
                    Msg =
                        "Your account has been locked, please contact our administrator for more information", // todo: 转变成code以获取翻译
                    Success = false
                });
            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateToken(user.Id);
            var auth = _mapper.Map<Auth>(user);
            auth.Roles = await _userManager.GetRolesAsync(user);
            auth.ShippingAdressList = await _userRepository.GetUserShippingAdress(user.Id);
            auth.FacturationAdress = await _userRepository.GetUserFacturationAdress(user.Id);
            auth.UserId = user.Id;
            return Json(new ApiResult
            {
                Data = new
                {
                    Token = token,
                    User = auth,
                    UserId = user.Id
                },
                Msg = "OK",
                Success = true
            });
        }

        return Json(new ApiResult
        {
            Msg = "Your password or username is not correct please check your login information",
            Success = false
        });
    }


    // Generate the jwt toekn 
    private string GenerateToken(int userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp,
                new DateTimeOffset(DateTime.Now.AddDays(1)).ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            new JwtHeader(new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Value.JwtSecret)),
                SecurityAlgorithms.HmacSha256)),
            new JwtPayload(claims));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(int userId)
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}