using System.Threading.Tasks;

namespace JLSApplicationBackend.Services;

public interface ISendEmailAndMessageService
{
    Task<long> CreateOrUpdateOrderAsync(long orderId, string type);

    Task<int> ResetPasswordOrConfirmEmailLinkAsync(int userId, string link, string type);
    Task<int> AfterResetPasswordOrConfirmEmailLinkAsync(int userId, string type);

    Task<int> ClientMessageToAdminAsync(string clientEmail, string message);

    /// <summary>
    /// Dispatches all queued emails (Safety Net).
    /// </summary>
    Task SendQueuedEmails();

    /// <summary>
    /// Dispatches a specific email by ID. Used for immediate background jobs.
    /// </summary>
    Task SendSingleEmailAsync(long emailId);
}