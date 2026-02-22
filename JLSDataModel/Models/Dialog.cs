namespace JLSDataModel.Models;

public class Dialog : BaseObject
{
    public int FromUserId { get; set; }
    public int? ToUserId { get; set; }
    public string Message { get; set; }

    public bool IsReaded { get; set; }
}