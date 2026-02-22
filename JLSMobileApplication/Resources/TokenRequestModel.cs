namespace JLSApplicationBackend.Resources;

public class TokenRequestModel
{
    public string GrantType { get; set; } // password or refresh_token
    public string ClientId { get; set; }
    public string UserName { get; set; }
    public string RefreshToken { get; set; }
    public string Password { get; set; }
}