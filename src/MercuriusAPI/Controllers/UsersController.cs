using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Services.UserServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MercuriusAPI.Controllers
{
    /// <summary>
    /// Handles user management actions such as deleting users and assigning roles.
    /// </summary>
    /// <remarks>
    /// The UserController class is responsible for handling HTTP requests related to user management.
    /// This includes actions such as deleting a user, adding roles to a user, and changing a user's password.
    /// </remarks>
    [Authorize(Roles = "admin")]
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Gets a user by their unique ID.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>The user details.</returns>
        [HttpGet("{id}")]
        public Task<GetUserDTO> GetUserByIdAsync(int id)
            => _userService.GetUserByIdAsync(id);

        /// <summary>
        /// Gets all users in the system.
        /// </summary>
        /// <returns>A list of all users.</returns>
        [HttpGet]
        public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync()
            => _userService.GetAllUsersAsync();
        /// <summary>
        /// Deletes a user by username.
        /// </summary>
        /// <param name="username">The username of the user to delete.</param>
        [HttpDelete("{username}")]
        public Task DeleteUserAsync(string username)
            => _userService.DeleteUserAsync(username);

        /// <summary>
        /// Adds a role to a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="request">The request containing the role name.</param>
        [HttpPost("{username}/roles")]
        public Task AddRoleToUserAsync([FromRoute] string username, [FromBody] AddUserRoleRequest request)
            => _userService.AddRoleToUserAsync(username, request);


        [HttpDelete("{username}/roles/{role}")]
        public Task DeleteRoleFromUserAsync([FromRoute] string username, [FromRoute] string role)
            => _userService.DeleteRoleFromUserAsync(username, role);
        /// <summary>
        /// Changes the password of a user.
        /// </summary>
        /// <param name="username">The username of the user whose password is to be changed.</param>
        /// <param name="request">The request containing the new password.</param>
        [HttpPatch("{username}/password")]
        public Task ChangePasswordAsync([FromRoute] string username, [FromBody] ChangePasswordRequest request)
        {
            var authenticatedUsername = User.FindFirstValue(ClaimTypes.Name);

            if (authenticatedUsername == null || !authenticatedUsername.Equals(username, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("You are not authorized to change this user's password.");

            return _userService.ChangePasswordAsync(username, request);
        }
    }
}
