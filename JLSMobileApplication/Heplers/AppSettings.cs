namespace JLSApplicationBackend.Heplers;

public class AppSettings
{
    public string SendGridKey { get; set; }
    public string SendGridUser { get; set; }
    public string EmailAccount { get; set; }
    public string EmailPassword { get; set; }
    public string MyAllowSpecificOrigins { get; set; }
    public int EmailPort { get; set; }
    public string EmailHost { get; set; }
    public string JwtSecret { get; set; }
    public string JwtIssuer { get; set; }
    public string JwtAudience { get; set; }

    public string AllowedOrigins { get; set; }

    // Properties for JWT Token Signature
    public string Site { get; set; }
    public string Audience { get; set; }
    public string ExpireTime { get; set; }
    public string Secret { get; set; }

    // Token Refresh Properties added 
    public string RefreshToken { get; set; }
    public string GrantType { get; set; }
    public string ClientId { get; set; }
    public string WebSiteUrl { get; set; }
    public string SuperAdminList { get; set; }
    public string AdminInitialPassword { get; set; }

    public string ExportPath { get; set; }
    public string ImagePath { get; set; }

    public string RedirectEmailTo { get; set; }
}