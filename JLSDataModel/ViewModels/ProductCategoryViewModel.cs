namespace JLSDataModel.ViewModels;

public class ProductCategoryViewModel
{
    public ProductCategoryViewModel()
    {
        Reference = new SubProductCategoryViewModel();
    }

    public SubProductCategoryViewModel Reference { get; set; }
    public int TotalCount { get; set; }
    public long ReferenceId { get; set; }
}

public class SubProductCategoryViewModel
{
    public long ReferenceId { get; set; }
    public string Code { get; set; }
    public string Label { get; set; }
}