using System.Threading.Tasks;

namespace JLSApplicationBackend.Services;

public interface ISendEmailAndMessageService
{
    Task<long> CreateOrUpdateOrderAsync(long OrderId, string Type);

    Task<int> ResetPasswordOuConfirmEmailLinkAsync(int UserId, string Link, string Type);
    Task<int> AfterResetPasswordOuConfirmEmailLinkAsync(int UserId, string Type);

    Task<int> ClientMessageToAdminAsync(string ClientEmail, string Message);
    int SendAdvertisement(string Type);

    void SendEmailInDb();
}