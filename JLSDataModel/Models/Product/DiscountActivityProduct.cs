namespace JLSDataModel.Models.Product;

public class DiscountActivityProduct : BaseObject
{
    public long DiscountActivityId { get; set; }
    public DiscountActivity DiscountActivity { get; set; }

    public long ProductId { get; set; }

    public Product Product { get; set; }
}