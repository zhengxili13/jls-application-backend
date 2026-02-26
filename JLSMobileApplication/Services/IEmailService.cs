using System.Threading.Tasks;

namespace JLSApplicationBackend.Services;

public interface IEmailService
{
    Task<string> SendEmailAsync(string toEmail, string subject, string htmlBody, string attachmentPath = null);
}