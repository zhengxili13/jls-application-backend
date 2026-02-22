using System.Collections.Generic;
using System.Threading.Tasks;
using JLSDataModel.Models.Adress;
using JLSDataModel.Models.User;

namespace JLSDataAccess.Interfaces;

public interface IUserRepository
{
    Task<long> InsertSubscribeEmail(string Email);
    Task<bool> CheckUserIsAlreadyExist(string Username);
    Task<long> UpdateReadedDialog(int UserId);
    Task<List<dynamic>> GetNoReadedDialog(int UserId);

    Task<List<dynamic>> GetNoReadedDialogClient(int UserId);
    Task<long> InsertDialog(string Message, int FromUserId, int? ToUserId);
    Task<List<dynamic>> GetChatedUser(int UserId);
    Task<List<dynamic>> GetChatDialog(int UserId, int? AdminUserId);
    Task<Adress> GetUserFacturationAdress(int userId);

    Task<List<Adress>> GetUserShippingAdress(int userId);

    Task<List<User>> GetUserListByRole(List<string> Roles);


    Task<List<dynamic>> AdvancedUserSearch(int? UserType, bool? Validity, string Username);

    Task<List<dynamic>> GetUserRoleList();
    Task<dynamic> GetUserById(int UserId);

    Task<dynamic> CreateOrUpdateUser(int UserId, string Email, string Password, int RoleId, bool Validity,
        bool EmailConfirmed);


    Task<long> UpdateUserInfo(int UserId, string EntrepriseName, string Siret, string PhoneNumber,
        long? DefaultShippingAddressId);
}