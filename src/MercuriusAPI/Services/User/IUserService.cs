using MercuriusAPI.DTOs.Auth;
using System.Threading.Tasks;

namespace MercuriusAPI.Services.User
{
    public interface IUserService
    {
        Task DeleteUserAsync(string username);
        Task AddRoleToUserAsync(AddUserRoleRequest request);
    }
}
