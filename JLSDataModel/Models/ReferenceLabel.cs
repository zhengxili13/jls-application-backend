namespace JLSDataModel.Models;

public class ReferenceLabel : BaseObject
{
    public string Label { get; set; }
    public string Lang { get; set; }


    public long ReferenceItemId { get; set; }
    public ReferenceItem ReferenceItem { get; set; }
}