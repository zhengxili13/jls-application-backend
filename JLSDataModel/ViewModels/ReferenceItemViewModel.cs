namespace JLSDataModel.ViewModels;

public class ReferenceItemViewModelMobile
{
    public ReferenceItemViewModelMobile()
    {
        ReferenceParent = new ReferenceItemViewModelMobile();
    }

    public long Id { get; set; }
    public string Code { get; set; }
    public long? ParentId { get; set; }
    public string Value { get; set; }
    public int? Order { get; set; }
    public long ReferenceCategoryId { get; set; }
    public string ReferenceCategoryLabel { get; set; }
    public string ReferenceCategoryLongLabel { get; set; }
    public string Label { get; set; }

    public ReferenceItemViewModelMobile ReferenceParent { get; set; }
    public bool? Validity { get; set; }
}