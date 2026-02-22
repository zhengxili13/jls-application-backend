using System.Collections.Generic;
using JLSDataModel.Models;
using JLSDataModel.Models.Adress;

namespace JLSDataModel.AdminViewModel;

public class OrderViewModel
{
    public string OrderReferenceCode { get; set; }

    public string PaymentInfo { get; set; }

    public string ClientRemark { get; set; }

    public string AdminRemark { get; set; }

    public float? TotalPrice { get; set; }

    public float? TaxRate { get; set; }

    // Foreign key 
    public UserViewModel User { get; set; }

    public ReferenceItem StatusReferenceItem { get; set; }

    public string StatusLabel { get; set; }

    public Adress ShippingAdress { get; set; }

    public Adress FacturationAdress { get; set; }

    public List<OrderProductViewModel> Products { get; set; }
}