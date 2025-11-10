using MercuriusAPI.DTOs.Auth;

namespace MercuriusAPI.Services.UserServices;

public interface IUserService
{
    Task DeleteUserAsync(string username);
    Task AddRoleToUserAsync(string username, AddUserRoleRequest request);
    Task ChangePasswordAsync(string username, ChangePasswordRequest newPassword);
    Task<IEnumerable<GetUserDTO>> GetAllUsersAsync();
    Task<GetUserDTO> GetUserByIdAsync(int id);
    Task DeleteRoleFromUserAsync(string username, string roleName);
    Task SeedInitialUserAsync(IConfiguration configuration);
}
