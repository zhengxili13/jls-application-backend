namespace JLSDataModel.Models.Audit;

public class Audit : BaseObject
{
    public int AuditType { get; set; }
    public string TableName { get; set; }
}