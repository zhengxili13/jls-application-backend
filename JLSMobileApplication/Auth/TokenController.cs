using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.Resources;
using JLSDataAccess;
using JLSDataModel.Models;
using JLSDataModel.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JLSMobileApplication.Auth;

[Route("api/[controller]/{action}/{id?}")]
[ApiController]
public class TokenController(
    UserManager<User> userManager,
    IOptions<AppSettings> appSettings,
    TokenModel token,
    JlsDbContext db,
    ILogger<TokenController> logger)
    : Controller
{
    // jwt and refresh tokens
    private readonly AppSettings _appSettings = appSettings.Value;

    private readonly TokenModel _token = token;

    [HttpPost]
    public async Task<IActionResult> Auth([FromBody] TokenRequestModel model) // granttype = "refresh_token"
    {
        // We will return Generic 500 HTTP Server Status Error
        // If we receive an invalid payload
        if (model == null) return new StatusCodeResult(500);
        // todo encry password when login and create account
        switch (model.GrantType)
        {
            case "password":
                return await GenerateNewToken(model);
            case "refresh_token":
                return await RefreshToken(model);
            default:
                // not supported - return a HTTP 401 (Unauthorized)
                return new UnauthorizedResult(); //BadRequestResult();
        }
    }


    // Method to Create New JWT and Refresh Token
    private async Task<IActionResult> GenerateNewToken(TokenRequestModel model)
    {
        // TODO CHECK front-end error message 
        // check if there's an user with the given username
        var user = await userManager.FindByNameAsync(model.UserName);

        // Validate credentials
        if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
        {
            // If the user has confirmed his email
            if (!await userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError(string.Empty, "User Has not Confirmed Email.");

                return NotFound(new { LoginError = "Msg_EmailNotValide" });
            }

            if (user.Validity == false)
            {
                ModelState.AddModelError(string.Empty, "Account is locked");

                return NotFound(new { LoginError = "Msg_AccountBloque" });
            }

            // username & password matches: create the refresh token
            var newRtoken = CreateRefreshToken(_appSettings.ClientId, user.Id);

            // first we delete any existing old refreshtokens
            var oldrTokens =
                db.TokenModel.Where(rt =>
                    rt.UserId == user.Id &&
                    rt.ExpiryTime <
                    DateTime.UtcNow); // Remove only the expired token, keep the possibility to login for mutiple platform in the same time 

            if (oldrTokens != null)
                foreach (var oldrt in oldrTokens)
                    db.TokenModel.Remove(oldrt);

            // Add new refresh token to Database
            db.TokenModel.Add(newRtoken);

            await db.SaveChangesAsync();

            // Create & Return the access token which contains JWT and Refresh Token

            var accessToken = await CreateAccessToken(user, newRtoken.Value);


            return Ok(new { authToken = accessToken }); // TODO: add refresh token expired 
        }

        ModelState.AddModelError("", "Username/Password was not Found");
        return NotFound(new { LoginError = "Msg_PasswordNotCorrect" });
    }

    // Create access Tokenm
    private async Task<TokenResponseModel> CreateAccessToken(User user, string refreshToken)
    {
        try
        {
            var tokenExpiryTime = Convert.ToDouble(_appSettings.ExpireTime);

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));

            var roles = await userManager.GetRolesAsync(user);

            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, "client"), //todo change
                    new Claim("LoggedOn", DateTime.Now.ToString())
                }),

                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
                //Issuer = _appSettings.Site,
                //Audience = _appSettings.Audience,
                Expires = DateTime.UtcNow.AddMinutes(tokenExpiryTime)
            };

            // Generate token

            var newtoken = tokenHandler.CreateToken(tokenDescriptor);

            var encodedToken = tokenHandler.WriteToken(newtoken);

            return new TokenResponseModel
            {
                token = encodedToken,
                expiration = newtoken.ValidTo,
                refresh_token = refreshToken,
                roles = roles[0],
                username = user.UserName,
                userId = user.Id,
                entrepriseName = user.EntrepriseName
            };
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            throw;
        }
    }

    private TokenModel CreateRefreshToken(string clientId, int userId)
    {
        var role = (from u in db.Users
            join ur in db.UserRoles on u.Id equals ur.UserId
            join r in db.Roles on ur.RoleId equals r.Id
            select r.Name).FirstOrDefault();
        return new TokenModel
        {
            ClientId = clientId,
            UserId = userId,
            Value = Guid.NewGuid().ToString("N"),
            CreatedDate = DateTime.UtcNow,
            ExpiryTime = role == null || role.Contains("Admin")
                ? DateTime.UtcNow.AddDays(1)
                : DateTime.UtcNow.AddDays(15)
        };
    }


    // Method to Refresh JWT and Refresh Token
    private async Task<IActionResult> RefreshToken(TokenRequestModel model)
    {
        try
        {
            // check if the received refreshToken exists for the given clientId
            var rt = db.TokenModel
                .FirstOrDefault(t =>
                    t.ClientId == _appSettings.ClientId
                    && t.Value == model.RefreshToken.ToString());


            if (rt == null)
                // refresh token not found or invalid (or invalid clientId)
                return new UnauthorizedResult(); //BadRequestResult();

            // check if refresh token is expired
            if (rt.ExpiryTime < DateTime.UtcNow) return new UnauthorizedResult(); //BadRequestResult();

            // check if there's an user with the refresh token's userId
            var user = await userManager.FindByIdAsync(rt.UserId.ToString());


            if (user == null)
                // UserId not found or invalid
                return new UnauthorizedResult(); // BadRequestResult();

            // generate a new refresh token 

            var rtNew = CreateRefreshToken(rt.ClientId, rt.UserId);

            // invalidate the old refresh token (by deleting it)
            db.TokenModel.Remove(rt);

            // add the new refresh token
            db.TokenModel.Add(rtNew);

            // persist changes in the DB
            await db.SaveChangesAsync();

            var response = await CreateAccessToken(user, rtNew.Value);

            return Ok(new { authToken = response });
        }
        catch (Exception e)
        {
            logger.LogError(e.Message);
            return new UnauthorizedResult();
        }
    }
}