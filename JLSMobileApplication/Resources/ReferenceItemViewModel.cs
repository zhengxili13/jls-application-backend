namespace JLSApplicationBackend.Resources;

public class ReferenceItemViewModel
{
    public long Id { get; set; }
    public string Code { get; set; }
    public long? ParentId { get; set; }

    public long ReferenceCategoryId { get; set; }
    public string ReferenceCategoryLabel { get; set; }
    public string Label { get; set; }

    public string Value { get; set; }
    public int? Order { get; set; }
    public bool? Validity { get; set; }
}