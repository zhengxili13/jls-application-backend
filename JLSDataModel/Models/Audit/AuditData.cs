namespace JLSDataModel.Models.Audit;

public class AuditData : BaseObject
{
    public long RowId { get; set; }
    public string FieldName { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public long AuditId { get; set; }
    public Audit Audit { get; set; }
}