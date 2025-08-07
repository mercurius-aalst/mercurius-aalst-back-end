using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Services.UserServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers
{
    /// <summary>
    /// Handles user management actions such as deleting users and assigning roles.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Deletes a user by username.
        /// </summary>
        /// <param name="username">The username of the user to delete.</param>
        [HttpDelete("{username}")]
        public Task DeleteUser(string username)
            => _userService.DeleteUserAsync(username);

        /// <summary>
        /// Adds a role to a user.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="request">The request containing the role name.</param>
        [HttpPost("{username}/roles")]
        public Task AddRoleToUser([FromRoute] string username, [FromBody] AddUserRoleRequest request)
            => _userService.AddRoleToUserAsync(new AddUserRoleRequest { Username = username, RoleName = request.RoleName });
    }
}
