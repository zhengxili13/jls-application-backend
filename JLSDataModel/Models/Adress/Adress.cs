namespace JLSDataModel.Models.Adress;

public class Adress : BaseObject
{
    public string ContactTelephone { get; set; }

    public string ContactFax { get; set; }

    public string ContactLastName { get; set; }

    public string ContactFirstName { get; set; }

    public string ZipCode { get; set; }

    public string FirstLineAddress { get; set; }

    public string SecondLineAddress { get; set; }
    public string City { get; set; }
    public string Provence { get; set; }

    public string Country { get; set; }
    public string EntrepriseName { get; set; }
    public bool? IsDefaultAdress { get; set; }

    public Adress Clone()
    {
        var cloneAddress = new Adress
        {
            ZipCode = ZipCode,
            ContactTelephone = ContactTelephone,
            ContactFax = ContactFax,
            ContactLastName = ContactLastName,
            ContactFirstName = ContactFirstName,
            SecondLineAddress = SecondLineAddress,
            FirstLineAddress = FirstLineAddress,
            City = City,
            EntrepriseName = EntrepriseName,
            Country = Country
        };

        return cloneAddress;
    }
}