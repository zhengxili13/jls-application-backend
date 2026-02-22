namespace JLSDataModel.Models;

public class ReferenceCategory : BaseObject
{
    public string ShortLabel { get; set; }
    public string LongLabel { get; set; }
    public bool? Validity { get; set; }
}