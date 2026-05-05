using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.Shared.DTOs.Auth;

namespace Mercurius.LAN.API.Services.UserServices;

public interface IUserService
{
    Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request);
    Task DeleteUserAsync(string username);
    Task DeleteUserByIdAsync(Guid id);
    Task AddRoleToUserAsync(string username, AddUserRoleRequest request);
    Task ChangePasswordAsync(string username, ChangePasswordRequest newPassword);
    Task<IEnumerable<GetUserDTO>> GetAllUsersAsync();
    Task<GetUserDTO> GetUserByIdAsync(Guid id);
    Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request);
    Task DeleteRoleFromUserAsync(string username, string roleName);
    Task SeedInitialUserAsync(IConfiguration configuration);
}
