using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using JLSApplicationBackend.Heplers;
using Microsoft.Extensions.Options;

namespace JLSApplicationBackend.Services;

public class EmailService : IEmailService
{
    /// <summary>
    ///     只用于测试目的请勿在生产环境中放入此代码
    /// </summary>
    private readonly AppSettings _appSettings;

    public EmailService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public string SendEmail(string ToEmail, string Subjet, string Message, string AttachmentPath)
    {
        try
        {
            // Credentials
            var credentials = new NetworkCredential(_appSettings.EmailAccount, _appSettings.EmailPassword);

            // Mail message
            var mail = new MailMessage
            {
                From = new MailAddress(_appSettings.EmailAccount),
                Subject = Subjet,
                Body = Message
            };
            mail.IsBodyHtml = true;
            mail.To.Add(new MailAddress(ToEmail));
            /* If has attachment */
            if (AttachmentPath != null)
            {
                var data = new Attachment(AttachmentPath, MediaTypeNames.Application.Octet);
                mail.Attachments.Add(data);
            }

            // Smtp client
            var client = new SmtpClient
            {
                Port = 587,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Host = "smtp.gmail.com",
                EnableSsl = true,
                Credentials = credentials
            };
            client.Send(mail);
            return "Email Sent Successfully!"; //todo change to code 
        }
        catch (Exception e)
        {
            return e.Message; // todo change to code 
        }
    }
}