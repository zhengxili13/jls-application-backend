using System;
using Microsoft.AspNetCore.Identity;

namespace JLSDataModel.Models.User;

public class User : IdentityUser<int>
{
    public User()
    {
        CreatedOn = DateTime.Now;
        Validity = true;
    }

    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public bool? Validity { get; set; }
    public string Siret { get; set; }
    public string EntrepriseName { get; set; }

    public long FacturationAdressId { get; set; }
    // public Adress.Adress FacturationAdress { get; set; }
}