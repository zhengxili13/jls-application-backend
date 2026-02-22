namespace JLSDataModel.Models.User;

public class UserPreferenceCategory : BaseObject
{
    public ReferenceCategory ReferenceCategory { get; set; }
    public long ReferenceCategoryId { get; set; }

    public User User { get; set; }
    public string UserId { get; set; }
}