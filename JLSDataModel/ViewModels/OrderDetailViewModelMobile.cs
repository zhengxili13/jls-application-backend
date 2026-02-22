using System.Collections.Generic;
using JLSDataModel.AdminViewModel;
using JLSDataModel.Models.Adress;
using JLSDataModel.Models.Order;

namespace JLSDataModel.ViewModels;

public class OrderDetailViewModelMobile
{
    public OrderDetailViewModelMobile()
    {
        OrderInfo = new OrderInfo();
        FacturationAdress = new Adress();
        ShippingAdress = new Adress();
        ProductList = new List<ProductDetailViewModelMobile>();
        Status = new ReferenceItemViewModel();
        StatusInfo = new object();
    }

    public ReferenceItemViewModel Status { get; set; }
    public OrderInfo OrderInfo { get; set; }
    public Adress FacturationAdress { get; set; }

    public dynamic StatusInfo { get; set; }

    public List<ProductDetailViewModelMobile> ProductList { get; set; }
    public Adress ShippingAdress { get; set; }
}