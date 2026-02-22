using System.Collections.Generic;
using JLSDataModel.Models;
using JLSDataModel.Models.Product;

namespace JLSDataModel.AdminViewModel;

public class ProductViewModel
{
    public long Id { get; set; }
    public List<ReferenceLabel> Label { get; set; }
    public string ReferenceCode { get; set; }
    public float? Price { get; set; }
    public int? QuantityPerBox { get; set; }
    public int? MinQuantity { get; set; }

    public long? Category { get; set; }

    public List<ProductPhotoPath> Images { get; set; }

    public long ReferenceItemId { get; set; }


    /* 不确定需要询问客户 */
    public string Color { get; set; }

    public string Material { get; set; }

    public string Size { get; set; }

    public string Description { get; set; }
    /* 不确定需要询问客户 */
}