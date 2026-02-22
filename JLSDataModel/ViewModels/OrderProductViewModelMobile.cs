namespace JLSDataModel.ViewModels;

public class OrderProductViewModelMobile
{
    public float? Price { get; set; }
    public long ReferenceId { get; set; }
    public int Quantity { get; set; }
    public int UnityQuantity { get; set; }
    public int QuantityPerBox { get; set; }
}