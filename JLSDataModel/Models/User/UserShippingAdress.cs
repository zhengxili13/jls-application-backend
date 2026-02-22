namespace JLSDataModel.Models.User;

public class UserShippingAdress : BaseObject
{
    public int UserId { get; set; }
    public User User { get; set; }

    public long ShippingAdressId { get; set; }

    public Adress.Adress ShippingAdress { get; set; }
}