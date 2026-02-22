namespace JLSDataModel.Models;

public class ReferenceItem : BaseObject
{
    public string Code { get; set; }

    public long? ParentId { get; set; }

    public string Value { get; set; }

    public int? Order { get; set; }

    public bool? Validity { get; set; }
    public long ReferenceCategoryId { get; set; }
    public ReferenceCategory ReferenceCategory { get; set; }
}