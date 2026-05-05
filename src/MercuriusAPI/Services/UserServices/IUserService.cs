using Mercurius.LAN.API.DTOs.Auth;

namespace Mercurius.LAN.API.Services.UserServices;

public interface IUserService
{
    Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request);
    Task<GetUserDTO> CompleteProfileAsync(string auth0Subject, CompleteUserProfileRequest request);
    Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0Subject);
    Task DeleteUserAsync(string username);
    Task DeleteUserByIdAsync(Guid id);
    Task<IEnumerable<GetUserDTO>> GetAllUsersAsync();
    Task<GetUserDTO> GetUserByIdAsync(Guid id);
    Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request);
}
