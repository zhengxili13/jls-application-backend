using System.ComponentModel.DataAnnotations;

namespace JLSDataModel.Models;

public class VisitorCounter
{
    [Key] public long? Id { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public long Counter { get; set; }
}