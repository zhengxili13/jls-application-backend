using System;

namespace JLSDataModel.Models.Order;

public class ShipmentInfo : BaseObject
{
    public string ShipmentNumber { get; set; }

    public string Weight { get; set; }

    public float? Fee { get; set; }

    public DateTime? Date { get; set; }
}