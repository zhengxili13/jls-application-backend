using System;

namespace JLSDataModel.Models.Order;

public class OrderInfoStatusLog : BaseObject
{
    public long OrderInfoId { get; set; }

    public OrderInfo OrderInfo { get; set; }

    public long? OldStatusId { get; set; }
    public long? NewStatusId { get; set; }
    public DateTime? ActionTime { get; set; }
    public int? UserId { get; set; }
}