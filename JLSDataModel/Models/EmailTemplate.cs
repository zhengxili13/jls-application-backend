namespace JLSDataModel.Models;

public class EmailTemplate : BaseObject
{
    public string Name { get; set; }

    public string Title { get; set; }
    public string Body { get; set; }
    public string MessageBody { get; set; }
}