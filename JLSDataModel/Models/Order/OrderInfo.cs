namespace JLSDataModel.Models.Order;

public class OrderInfo : BaseObject
{
    public string OrderReferenceCode { get; set; }

    public string PaymentInfo { get; set; }

    public long? ClientRemarkId { get; set; }

    public long? AdminRemarkId { get; set; }

    public float? TotalPrice { get; set; }
    public float? TotalPriceHT { get; set; }

    public long? TaxRateId { get; set; }

    public long OrderTypeId { get; set; }

    public long CustomerId { get; set; }

    public long? ShipmentInfoId { get; set; }

    // Foreign key 
    public int UserId { get; set; }
    public User.User User { get; set; }

    public long StatusReferenceItemId { get; set; }
    public ReferenceItem StatusReferenceItem { get; set; }

    public long ShippingAdressId { get; set; }
    public Adress.Adress ShippingAdress { get; set; }

    public long FacturationAdressId { get; set; }
    public Adress.Adress FacturationAdress { get; set; }
}