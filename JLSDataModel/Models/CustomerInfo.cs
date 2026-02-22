namespace JLSDataModel.Models;

public class CustomerInfo : BaseObject
{
    public string Email { get; set; }
    public string EntrepriseName { get; set; }

    public string PhoneNumber { get; set; }

    public string Siret { get; set; }

    public int? UserId { get; set; }

    public static CustomerInfo FromUserInfo(User.User user)
    {
        return new CustomerInfo
        {
            PhoneNumber = user.PhoneNumber,
            Siret = user.Siret,
            EntrepriseName = user.EntrepriseName,
            Email = user.Email,
            UserId = user.Id
        };
    }
}