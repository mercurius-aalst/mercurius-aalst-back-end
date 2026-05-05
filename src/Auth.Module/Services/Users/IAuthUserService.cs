using Auth.Module.Models;
using Microsoft.Extensions.Configuration;

namespace Auth.Module.Services.Users;

public interface IAuthUserService
{
    Task<Guid> CreateAuthUserAsync(string username, string password);
    Task<Guid> CreateExternalAuthUserAsync(string username);
    Task<GetAuthUserDTO> GetAuthUserByIdAsync(Guid id);
    Task UpdateUsernameAsync(Guid id, string username);
    Task DeleteAuthUserAsync(Guid id);
    Task AddRoleToUserAsync(Guid id, string roleName);
    Task RemoveRoleFromUserAsync(Guid id, string roleName);
    Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword);
    Task SeedInitialUserAsync(IConfiguration configuration);
}
