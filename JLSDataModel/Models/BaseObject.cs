using System;
using System.ComponentModel.DataAnnotations;

namespace JLSDataModel.Models;

public class BaseObject
{
    public BaseObject()
    {
        CreatedOn = DateTime.Now;
    }

    [Key] public long Id { get; set; }

    public DateTime? CreatedOn { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public long? UpdatedBy { get; set; }
}