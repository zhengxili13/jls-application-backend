namespace JLSDataModel.Models.Product;

public class ProductPhotoPath : BaseObject
{
    public string Path { get; set; }

    public long ProductId { get; set; }

    public bool? IsDefault { get; set; }

    public Product Product { get; set; }
}