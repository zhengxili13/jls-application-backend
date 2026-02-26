using System.Threading.Tasks;

namespace JLSApplicationBackend.Services;

public interface ISendEmailAndMessageService
{
    Task<long> CreateOrUpdateOrderAsync(long orderId, string type);

    Task<int> ResetPasswordOrConfirmEmailLinkAsync(int userId, string link, string type);
    Task<int> AfterResetPasswordOrConfirmEmailLinkAsync(int userId, string type);

    Task<int> ClientMessageToAdminAsync(string clientEmail, string message);

    /// <summary>
    /// Dispatches all queued emails in the EmailToSend table. Called by a background job.
    /// </summary>
    Task SendQueuedEmails();
}