using Mercurius.LAN.API.DTOs.Auth;

namespace Mercurius.LAN.API.Services.UserServices;

public interface IUserService
{
    Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request);
    Task<GetUserDTO> CompleteProfileAsync(string auth0UserId, CompleteUserProfileRequest request);
    Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0UserId);
    Task<PublicUserProfileDTO> GetPublicUserProfileByUsernameAsync(string username, bool includePlatformIds);
    Task<GetUserDTO> UpdateCurrentUserAsync(string auth0UserId, UpdateUserProfileRequest request);
    Task<UsernameAvailabilityResponse> CheckUsernameAvailabilityAsync(string auth0UserId, string username);
    Task<UserActionResponse> ResendVerificationEmailAsync(string auth0UserId);
    Task<UserActionResponse> SendPasswordResetEmailAsync(string auth0UserId);
    Task<UserActionResponse> AnonymizeCurrentUserAsync(string auth0UserId);
    Task DeleteUserAsync(string username);
    Task DeleteUserByIdAsync(Guid id);
    Task<IEnumerable<GetUserDTO>> GetAllUsersAsync();
    Task<GetUserDTO> GetUserByIdAsync(Guid id);
    Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request);
}
