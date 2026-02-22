using System;
using System.ComponentModel.DataAnnotations;

namespace JLSDataModel.Models;

public class TokenModel
{
    [Key] public int Id { get; set; }

    // The ClientId, where it comes from
    [Required] public string ClientId { get; set; }

    // Value of the Token
    [Required] public string Value { get; set; }

    // Get the Token Creation Date
    [Required] public DateTime CreatedDate { get; set; }

    [Required] public DateTime LastModifiedDate { get; set; }

    [Required] public DateTime ExpiryTime { get; set; }

    // The UserId it was issued to
    [Required] public int UserId { get; set; }

    public virtual User.User User { get; set; }
}