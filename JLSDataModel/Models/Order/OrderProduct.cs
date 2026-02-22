namespace JLSDataModel.Models.Order;

public class OrderProduct : BaseObject
{
    public double? TotalPrice { get; set; }
    public double? UnitPrice { get; set; }
    public int Quantity { get; set; }
    public long OrderId { get; set; }
    public int Colissage { get; set; }
    public OrderInfo OrderInfo { get; set; }
    public long ReferenceId { get; set; }
    public ReferenceItem Reference { get; set; }
}