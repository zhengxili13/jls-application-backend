namespace JLSDataModel.Models.Product;

public class DiscountActivity : BaseObject
{
    public string Title { get; set; }

    public string Description { get; set; }

    public float DiscountPercentage { get; set; }

    public bool? Validity { get; set; }
}