using System;
using System.Collections.Generic;
using JLSDataModel.AdminViewModel;
using JLSDataModel.Models;
using JLSDataModel.Models.Adress;
using JLSDataModel.Models.Order;

namespace JLSDataModel.ViewModels;

public class OrderFullDetailDto
{
    public OrderInfo OrderInfo { get; set; }
    public string ShippingMessage { get; set; }
    public Remark ClientRemark { get; set; }
    public Remark AdminRemark { get; set; }
    public ShipmentInfo ShipmentInfo { get; set; }
    public TaxRateDto TaxRate { get; set; }
    public CustomerInfo CustomerInfo { get; set; }
    public OrderTypeDto OrderType { get; set; }
    public ReferenceItemViewModel Status { get; set; }
    public List<OrderStatusInfoDto> StatusInfo { get; set; }
    public Adress FacturationAdress { get; set; }
    public Adress ShippingAdress { get; set; }
    public List<OrderProductDto> ProductList { get; set; }
}

public class TaxRateDto
{
    public long Id { get; set; }
    public string Code { get; set; }
    public string Value { get; set; }
}

public class OrderTypeDto
{
    public long Id { get; set; }
    public string Code { get; set; }
    public string Label { get; set; }
}

public class OrderStatusInfoDto
{
    public long Id { get; set; }
    public StatusReferenceDto OldStatus { get; set; }
    public StatusReferenceDto NewStatus { get; set; }
    public DateTime? ActionTime { get; set; }
    public int? UserId { get; set; }
    public string UserName { get; set; }
}

public class StatusReferenceDto
{
    public long ReferenceId { get; set; }
    public string Code { get; set; }
    public string Label { get; set; }
}

public class OrderProductDto
{
    public int UnityQuantity { get; set; }
    public int Quantity { get; set; }
    public long ProductId { get; set; }
    public long ReferenceId { get; set; }
    public string Code { get; set; }
    public long? ParentId { get; set; }
    public string Value { get; set; }
    public int? Order { get; set; }
    public string Label { get; set; }
    public double? Price { get; set; }
    public double? TotalPrice { get; set; }
    public bool IsModifiedPriceOrBox { get; set; }
    public int? QuantityPerBox { get; set; }
    public int? QuantityPerParcel { get; set; }
    public int? MinQuantity { get; set; }
    public string Size { get; set; }
    public string Color { get; set; }
    public string Material { get; set; }
    public string DefaultPhotoPath { get; set; }
    public List<ProductListPhotoPath> PhotoPath { get; set; }
}
