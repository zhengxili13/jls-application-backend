using System;

namespace JLSDataModel.Models.Product;

public class Product : BaseObject
{
    public DateTime? ActualCreatedOn { get; set; }
    public float? Price { get; set; }
    public int? QuantityPerBox { get; set; }

    public int? QuantityPerParcel { get; set; }
    public int? MinQuantity { get; set; }

    public long? TaxRateId { get; set; } // Link with ri

    public string Color { get; set; }

    public string Material { get; set; }

    public string Size { get; set; }

    public string Description { get; set; }

    public string Forme { get; set; }

    public string BarreCode { get; set; }
    public float? PreviousPrice { get; set; }

    public long ReferenceItemId { get; set; }
    public ReferenceItem ReferenceItem { get; set; }
}