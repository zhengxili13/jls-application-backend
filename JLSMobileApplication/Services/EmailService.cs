using System;
using System.IO;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace JLSApplicationBackend.Services;

public class MailkitEmailService : IEmailService
{
    private readonly AppSettings _appSettings;
    private readonly ILogger<MailkitEmailService> _logger;
    private readonly ICloudflareR2Service _cloudflareR2Service;

    public MailkitEmailService(IOptions<AppSettings> appSettings, ILogger<MailkitEmailService> logger, ICloudflareR2Service cloudflareR2Service)
    {
        _appSettings = appSettings.Value;
        _logger = logger;
        _cloudflareR2Service = cloudflareR2Service;
    }

    public async Task<string> SendEmailAsync(string toEmail, string subject, string htmlBody, string attachmentPath = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("JLS IMPORT", _appSettings.EmailAccount));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

        if (!string.IsNullOrEmpty(attachmentPath))
        {
            try
            {
                // attachmentPath is assumed to be in the "folder/filename" format (e.g., "Exports/123.pdf")
                var driveFile = await _cloudflareR2Service.DownloadFileAsync(attachmentPath);
                bodyBuilder.Attachments.Add(driveFile.FileName, driveFile.FileStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load attachment from Cloudflare R2 for path/Key: {AttachmentPath}", attachmentPath);
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(_appSettings.EmailHost, _appSettings.EmailPort, SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(_appSettings.EmailAccount, _appSettings.EmailPassword);
            await client.SendAsync(message);

            _logger.LogInformation("Email sent to {ToEmail} with subject '{Subject}'", toEmail, subject);
            return "Email Sent Successfully!";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send email to {ToEmail}", toEmail);
            return e.Message;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true);
        }
    }
}