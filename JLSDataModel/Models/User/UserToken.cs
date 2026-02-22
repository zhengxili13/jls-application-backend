using System;

namespace JLSDataModel.Models.User;

public class UserToken : BaseObject
{
    public string Token { get; set; }
    public DateTime Expires { get; set; }

    public bool Active { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }
}