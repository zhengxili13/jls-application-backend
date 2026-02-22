using System;
using JLSApplicationBackend.Heplers;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace JLSApplicationBackend.Services;

public class MailkitEmailService : IEmailService
{
    private readonly AppSettings _appSettings;

    public MailkitEmailService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public string SendEmail(string ToEmail, string Subjet, string Message, string AttachmentPath)
    {
        try
        {
            var message = new MimeMessage();

            var from = new MailboxAddress("JLS IMPORT",
                _appSettings.EmailAccount);
            message.From.Add(from);

            var to = new MailboxAddress(ToEmail,
                ToEmail);
            message.To.Add(to);

            message.Subject = Subjet;
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = Message;

            if (AttachmentPath != null) bodyBuilder.Attachments.Add(AttachmentPath);

            message.Body = bodyBuilder.ToMessageBody();


            var client = new SmtpClient();
            client.Connect(_appSettings.EmailHost, _appSettings.EmailPort, true);
            client.Authenticate(_appSettings.EmailAccount, _appSettings.EmailPassword);

            client.Send(message);
            client.Disconnect(true);
            client.Dispose();

            return "Email Sent Successfully!"; //todo change to code 
        }
        catch (Exception e)
        {
            return e.Message; // todo change to code 
        }
    }
}