namespace JLSDataModel.Models.Message;

public class MessageDestination : BaseObject
{
    public long MessageId { get; set; }
    public int? FromUserId { get; set; }
    public int? ToUserId { get; set; }
}