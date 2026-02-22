using System;

namespace JLSDataModel.Models.Analytics;

public class BestClientWidget
{
    public long UserId { get; set; }
    public string EntrepriseName { get; set; }
    public string ClientEmail { get; set; }
    public decimal TotalConsumationHT { get; set; }
    public DateTime LastPurchaseDate { get; set; }
    public decimal LastPurchaseAmount { get; set; }
}