using System.Collections.Generic;
using JLSDataModel.Models.Adress;

namespace JLSApplicationBackend.Auth;

public class Auth
{
    public int UserId { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }


    public IList<string> Roles { get; set; }
    public string Siret { get; set; }
    public string EntrepriseName { get; set; }


    public Adress FacturationAdress { get; set; }

    public List<Adress> ShippingAdressList { get; set; }

    public bool? Validity { get; set; }
}