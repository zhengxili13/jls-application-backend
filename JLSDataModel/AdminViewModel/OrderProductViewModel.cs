namespace JLSDataModel.AdminViewModel;

public class OrderProductViewModel
{
    public long? Id { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }
    public string ReferenceCode { get; set; }
    public float? Price { get; set; }
    public int Quantity { get; set; }
}