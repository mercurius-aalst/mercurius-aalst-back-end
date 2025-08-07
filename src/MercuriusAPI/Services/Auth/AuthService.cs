using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Data;
using MercuriusAPI.Models;
using MercuriusAPI.Models.Auth;
using Microsoft.EntityFrameworkCore;
using MercuriusAPI.Exceptions;
using System.Threading.Tasks;
using System.Linq;

namespace MercuriusAPI.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly MercuriusDBContext _dbContext;
        private readonly TokenService _tokenService;
        private readonly ILoginAttemptService _loginAttemptService;

        public AuthService(MercuriusDBContext dbContext, ILoginAttemptService loginAttemptService, TokenService tokenService)
        {
            _dbContext = dbContext;
            _loginAttemptService = loginAttemptService;
            _tokenService = tokenService;
        }

        public async Task RegisterAsync(LoginRequest request)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
                throw new ValidationException("Username already exists");
            PasswordHelper.CreatePasswordHash(request.Password, out var hash, out var salt);
            var user = new User { Username = request.Username, PasswordHash = hash, Salt = salt };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<AuthTokenResponse> LoginAsync(LoginRequest request)
        {
            var now = System.DateTime.UtcNow;
            if (_loginAttemptService.IsLockedOut(request.Username, now))
                throw new LockoutException();

            var user = await _dbContext.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash, user.Salt))
            {
                var attemptsLeft = _loginAttemptService.RegisterFailedAttempt(request.Username, now);
                if (attemptsLeft == 0)
                    throw new LockoutException();
                throw new InvalidCredentialsException($"Invalid username or password. {attemptsLeft} attempt(s) left before lockout.");
            }
            _loginAttemptService.Reset(request.Username);

            var jwtToken = _tokenService.GenerateJwtToken(user);
            var refreshTokenEntity = _tokenService.GenerateRefreshToken(user.Id);
            user.RefreshTokens.Add(refreshTokenEntity);
            var tokensToRemove = user.RefreshTokens.Where(rt => rt.Expires < System.DateTime.UtcNow).ToList();
            foreach (var oldToken in tokensToRemove)
            {
                user.RefreshTokens.Remove(oldToken);
                _dbContext.RefreshTokens.Remove(oldToken);
            }
            await _dbContext.SaveChangesAsync();

            return new AuthTokenResponse { Token = jwtToken, RefreshToken = refreshTokenEntity.Token };
        }

        public async Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var refreshToken = await _dbContext.RefreshTokens.Include(rt => rt.User)
                .ThenInclude(u => u.Roles)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
            if (refreshToken == null || refreshToken.Expires < System.DateTime.UtcNow)
                throw new InvalidCredentialsException();

            _dbContext.RefreshTokens.Remove(refreshToken);
            var newRefreshTokenEntity = _tokenService.GenerateRefreshToken(refreshToken.UserId);
            refreshToken.User.RefreshTokens.Add(newRefreshTokenEntity);
            var jwtToken = _tokenService.GenerateJwtToken(refreshToken.User);
            await _dbContext.SaveChangesAsync();
            return new AuthTokenResponse { Token = jwtToken, RefreshToken = newRefreshTokenEntity.Token };
        }

        public async Task RevokeRefreshTokenAsync(RevokeTokenRequest request)
        {
            var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);
            if (refreshToken == null)
                throw new NotFoundException("Refresh token not found.");
            _dbContext.RefreshTokens.Remove(refreshToken);
            await _dbContext.SaveChangesAsync();
        }
    }
}
