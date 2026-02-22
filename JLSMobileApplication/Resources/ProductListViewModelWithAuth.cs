using System.Collections.Generic;

namespace JLSApplicationBackend.Resources;

public class ProductListViewModelWithAuth
{
    public ProductListViewModelWithAuth()
    {
        PhotoPath = new List<ProductListPhotoPathView>();
    }

    public long ReferenceId { get; set; }
    public long ProductId { get; set; }
    public string Code { get; set; }
    public long? ParentId { get; set; }
    public string Value { get; set; }
    public int? Order { get; set; }
    public string Label { get; set; }
    public int? QuantityPerBox { get; set; }

    public float? Price { get; set; }
    public int? MinQuantity { get; set; }

    public List<ProductListPhotoPathView> PhotoPath { get; set; }
}