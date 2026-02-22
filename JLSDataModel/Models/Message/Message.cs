namespace JLSDataModel.Models.Message;

public class Message : BaseObject
{
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public long? OrderId { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public bool? IsReaded { get; set; }
}