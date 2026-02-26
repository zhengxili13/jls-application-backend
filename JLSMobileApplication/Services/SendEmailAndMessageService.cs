using System.Linq;
using System.Threading.Tasks;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.Services.EmailTemplateModel;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using JLSDataModel.Models;
using JLSDataModel.Models.Message;
using JLSDataModel.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace JLSApplicationBackend.Services;

public class SendEmailAndMessageService : ISendEmailAndMessageService
{
    private readonly AppSettings _appSettings;
    private readonly IEmailService _email;
    private readonly IExportService _exportService;
    private readonly IMessageRepository _messageRepository;
    private readonly UserManager<User> _userManager;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly JlsDbContext _db;

    public SendEmailAndMessageService(
        IOptions<AppSettings> appSettings,
        JlsDbContext context,
        IEmailService email,
        UserManager<User> userManager,
        IMessageRepository messageRepository,
        IExportService export,
        IEmailTemplateRenderer templateRenderer)
    {
        _appSettings = appSettings.Value;
        _db = context;
        _email = email;
        _userManager = userManager;
        _messageRepository = messageRepository;
        _exportService = export;
        _templateRenderer = templateRenderer;
    }

    public async Task<long> CreateOrUpdateOrderAsync(long orderId, string type)
    {
        var adminEmails = (from u in _db.Users
            join ur in _db.UserRoles on u.Id equals ur.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            where r.Name == "SuperAdmin" && u.Validity == true
            select u.Email).ToList();

        var emailModelClient = _db.EmailTemplate.FirstOrDefault(p => p.Name == type + "_Client");
        var emailModelAdmin = _db.EmailTemplate.FirstOrDefault(p => p.Name == type + "_Admin");
        var order = _db.OrderInfo.Find(orderId);

        if (order == null || emailModelClient == null || emailModelAdmin == null)
            return 0;

        var orderType = _db.ReferenceItem.FirstOrDefault(p => p.Id == order.OrderTypeId);

        // Internal orders also notify the operator
        if (orderType?.Code == "OrderType_Internal")
        {
            var operatorEmail = _db.Users
                .Where(p => p.Id == order.UserId)
                .Select(p => p.Email)
                .FirstOrDefault();

            if (operatorEmail != null)
                adminEmails.Add(operatorEmail);
        }

        var customerInfo = _db.CustomerInfo.FirstOrDefault(p => p.Id == order.CustomerId);
        if (customerInfo == null)
            return 0;

        string emailClientBody;
        string emailAdminBody;
        var messageClientText = emailModelClient.MessageBody;

        if (type == "CreateNewOrder")
        {
            messageClientText = messageClientText.Replace("{numerodecommande} ", order.Id.ToString());
            emailClientBody = await _templateRenderer.RenderAsync(new NewOrderClientContext(customerInfo.Email, order.Id.ToString()));
            emailAdminBody = await _templateRenderer.RenderAsync(new NewOrderAdminContext(customerInfo.Email, order.Id.ToString()));
        }
        else if (type == "UpdateOrder")
        {
            emailClientBody = await _templateRenderer.RenderAsync(new ModifyOrderClientContext(customerInfo.Email, order.Id.ToString()));
            emailAdminBody = await _templateRenderer.RenderAsync(new ModifyOrderAdminContext(customerInfo.Email, order.Id.ToString()));
        }
        else
        {
            emailClientBody = emailModelClient.Body;
            emailAdminBody = emailModelAdmin.Body;
        }

        // External orders: push an in-app message to the client
        if (orderType?.Code == "OrderType_External")
        {
            var inAppMessage = new Message
            {
                Title = emailModelClient.Title,
                Body = messageClientText,
                IsReaded = false
            };
            await _messageRepository.CreateMessage(inAppMessage, null, order.UserId);
        }

        // Generate invoice PDF (TODO: make language configurable)
        var pdfPath = await _exportService.ExportPdf(order.Id, "Fr");

        // Queue emails to the DB for background dispatch
        if (customerInfo.Email != null)
            await PushEmailIntoDb(customerInfo.Email, emailModelClient.Title, emailClientBody, pdfPath);

        foreach (var admin in adminEmails)
            if (admin != null)
                await PushEmailIntoDb(admin, emailModelAdmin.Title, emailAdminBody, pdfPath);

        return order.Id;
    }

    public async Task<int> ResetPasswordOrConfirmEmailLinkAsync(int userId, string link, string type)
    {
        var emailTemplate = _db.EmailTemplate.FirstOrDefault(p => p.Name == type);
        var user = _db.Users.Find(userId);

        if (emailTemplate == null || user == null || user.Email == null)
            return 0;

        EmailTemplateContext templateContext = type switch
        {
            "EmailConfirmation" => new EmailConfirmationContext(user.Email, link, user.EntrepriseName, user.PhoneNumber),
            "ResetPassword" => new ResetPasswordContext(user.Email, link, user.EntrepriseName, user.PhoneNumber),
            _ => null
        };

        var emailBody = templateContext != null
            ? await _templateRenderer.RenderAsync(templateContext)
            : emailTemplate.Body.Replace("{email}", user.Email).Replace("{username}", user.Email).Replace("{Link}", link);

        await PushEmailIntoDb(user.Email, emailTemplate.Title, emailBody, null);
        return user.Id;
    }

    public async Task<int> AfterResetPasswordOrConfirmEmailLinkAsync(int userId, string type)
    {
        // Previously broken: forgot to assign FirstOrDefault result — now fixed.
        var emailTemplate = _db.EmailTemplate.FirstOrDefault(p => p.Name == type);
        var user = _db.Users.Find(userId);

        if (emailTemplate == null || user == null || user.Email == null)
            return 0;

        EmailTemplateContext templateContext = type switch
        {
            "AfterResetPassword" => new AfterResetPasswordContext(),
            "AfterEmailConfirmation" => new AfterEmailConfirmationContext(),
            _ => null
        };

        var emailBody = templateContext != null
            ? await _templateRenderer.RenderAsync(templateContext)
            : emailTemplate.Body;

        await PushEmailIntoDb(user.Email, emailTemplate.Title, emailBody, null);
        return user.Id;
    }

    public async Task<int> ClientMessageToAdminAsync(string clientEmail, string message)
    {
        var adminEmails = (from u in _db.Users
            join ur in _db.UserRoles on u.Id equals ur.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            where u.Validity == true && r.Name == "SuperAdmin"
            select u.Email).ToList();

        if (clientEmail == null || adminEmails.Count == 0)
            return 0;

        var emailBody = await _templateRenderer.RenderAsync(new ClientMessageToAdminContext(clientEmail, message));

        foreach (var adminEmail in adminEmails)
            await PushEmailIntoDb(adminEmail, "Nouveau message", emailBody, null);

        return 1;
    }

    public void SendQueuedEmails()
    {
        var emailsToSend = _db.EmailToSend
            .Where(p => (p.IsSended == false || p.IsSended == null) && !p.ToEmail.Contains("@jls.com"))
            .ToList();

        if (emailsToSend.Count == 0)
            return;

        foreach (var email in emailsToSend)
        {
            // Redirect all outbound emails to a test address when configured (non-production)
            if (!string.IsNullOrEmpty(_appSettings.RedirectEmailTo))
            {
                email.Title = $"{email.Title} ({email.ToEmail})";
                email.ToEmail = _appSettings.RedirectEmailTo;
            }

            var sendResult = _email.SendEmail(email.ToEmail, email.Title, email.Body, email.Attachment);
            email.IsSended = true;
            email.Message = sendResult;
            _db.Update(email);
        }

        _db.SaveChanges();
    }

    public async Task<int> PushEmailIntoDb(string toEmail, string title, string body, string attachmentPath)
    {
        var email = new EmailToSend
        {
            ToEmail = toEmail,
            Title = title,
            Body = body,
            Attachment = attachmentPath,
            IsSended = false
        };

        await _db.AddAsync(email);
        await _db.SaveChangesAsync();
        return 1;
    }
}