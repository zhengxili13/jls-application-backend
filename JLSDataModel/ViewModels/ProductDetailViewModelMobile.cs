using System.Collections.Generic;
using JLSDataModel.AdminViewModel;
using JLSDataModel.Models.Product;

namespace JLSDataModel.ViewModels;

public class ProductDetailViewModelMobile : Product
{
    public ProductDetailViewModelMobile()
    {
        Reference = new ReferenceItemViewModel();
        PhotoPaths = new List<ProductPhotoPath>();
    }

    public int Quantity { get; set; }
    public ReferenceItemViewModel Reference { get; set; }

    public List<ProductPhotoPath> PhotoPaths { get; set; }
}