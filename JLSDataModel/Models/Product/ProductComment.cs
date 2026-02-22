namespace JLSDataModel.Models.Product;

public class ProductComment : BaseObject
{
    public string Title { get; set; }
    public string Body { get; set; }
    public int Level { get; set; }
    public int UserId { get; set; }
    public User.User User { get; set; }
    public long ProductId { get; set; }
    public Product Product { get; set; }
}