namespace JLSDataModel.Models.Order;

public class OrderInfoLog : BaseObject
{
    public string ChangedDescription { get; set; }

    public long OrderInfoId { get; set; }

    public OrderInfo OrderInfo { get; set; }
}