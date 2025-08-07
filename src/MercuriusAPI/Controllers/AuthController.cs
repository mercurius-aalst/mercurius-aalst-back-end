using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Services.Auth;

namespace MercuriusAPI.Controllers
{
    /// <summary>
    /// Handles authentication-related actions such as registration, login, token refresh, and user deletion.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="request">The registration request containing username and password.</param>
        /// <returns>Result of the registration operation.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public Task<IActionResult> Register([FromBody] LoginRequest request)
            => _authService.RegisterAsync(request);

        /// <summary>
        /// Authenticates a user and returns a JWT and refresh token.
        /// </summary>
        /// <param name="request">The login request containing username and password.</param>
        /// <returns>JWT and refresh token if authentication is successful.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public Task<IActionResult> Login([FromBody] LoginRequest request)
            => _authService.LoginAsync(request);

        /// <summary>
        /// Refreshes the JWT using a valid refresh token.
        /// </summary>
        /// <param name="request">The refresh token request.</param>
        /// <returns>New JWT and refresh token if the refresh token is valid.</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
            => _authService.RefreshTokenAsync(request);

        /// <summary>
        /// Revokes (deletes) a refresh token.
        /// </summary>
        /// <param name="request">The revoke token request.</param>
        /// <returns>Result of the revoke operation.</returns>
        [HttpPost("revoke")]
        public Task<IActionResult> Revoke([FromBody] RevokeTokenRequest request)
            => _authService.RevokeRefreshTokenAsync(request);

        /// <summary>
        /// Deletes a user by username.
        /// </summary>
        /// <param name="username">The username of the user to delete.</param>
        /// <returns>Result of the delete operation.</returns>
        [HttpDelete("{username}")]
        public Task<IActionResult> DeleteUser(string username)
            => _authService.DeleteUserAsync(username);
    }
}
