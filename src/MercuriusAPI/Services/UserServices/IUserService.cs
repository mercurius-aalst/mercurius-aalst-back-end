using Mercurius.LAN.API.DTOs.Auth;

namespace Mercurius.LAN.API.Services.UserServices;

public interface IUserService
{
    Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request);
    Task DeleteUserAsync(string username);
    Task DeleteUserByIdAsync(int id);
    Task AddRoleToUserAsync(string username, AddUserRoleRequest request);
    Task ChangePasswordAsync(string username, ChangePasswordRequest newPassword);
    Task<IEnumerable<GetUserDTO>> GetAllUsersAsync();
    Task<GetUserDTO> GetUserByIdAsync(int id);
    Task<GetUserDTO> UpdateUserAsync(int id, UpdateUserProfileRequest request);
    Task DeleteRoleFromUserAsync(string username, string roleName);
    Task SeedInitialUserAsync(IConfiguration configuration);
}
