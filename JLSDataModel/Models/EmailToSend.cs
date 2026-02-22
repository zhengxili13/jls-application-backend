namespace JLSDataModel.Models;

public class EmailToSend : BaseObject
{
    public string ToEmail { get; set; }
    public string CcEmail { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string Attachment { get; set; }
    public bool? IsSended { get; set; }
    public string Message { get; set; }
}