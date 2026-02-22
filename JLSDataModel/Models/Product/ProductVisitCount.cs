namespace JLSDataModel.Models.Product;

public class ProductVisitCount : BaseObject
{
    public long ProductId { get; set; }
    public long Count { get; set; }
}