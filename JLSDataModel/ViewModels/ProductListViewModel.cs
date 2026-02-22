using System.Collections.Generic;
using JLSDataModel.Models.Product;

namespace JLSDataModel.ViewModels;

public class ProductListViewModel
{
    public ProductListViewModel()
    {
        ProductListData = new List<ProductListData>();
    }

    public List<ProductListData> ProductListData { get; set; }
    public int TotalCount { get; set; }
}

public class ProductListPhotoPath
{
    public string Path { get; set; }
}

public class ProductListData
{
    public ProductListData()
    {
        PhotoPath = new List<ProductListPhotoPath>();

        Comments = new List<ProductComment>();
    }

    public int? NumberOfComment { get; set; }
    public long ReferenceId { get; set; }
    public long ProductId { get; set; }
    public string Code { get; set; }
    public long? ParentId { get; set; }
    public string Value { get; set; }
    public int? Order { get; set; }
    public string Label { get; set; }
    public float? Price { get; set; }
    public float? PreviousPrice { get; set; }
    public int? QuantityPerBox { get; set; }
    public int? QuantityPerParcel { get; set; }
    public int? MinQuantity { get; set; }

    public string DefaultPhotoPath { get; set; }
    public bool IsNew { get; set; }

    public List<ProductComment> Comments { get; set; }

    public List<ProductListPhotoPath> PhotoPath { get; set; }
}