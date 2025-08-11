using MercuriusAPI.Data;
using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.Auth;
using Microsoft.EntityFrameworkCore;
using MercuriusAPI.Services.Auth;

namespace MercuriusAPI.Services.UserServices
{
    public class UserService : IUserService
    {
        private readonly MercuriusDBContext _dbContext;

        public UserService(MercuriusDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task DeleteUserAsync(string username)
        {
            var normalizedUsername = username.Normalize();
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username.Normalize() == normalizedUsername);
            if (user == null)
                throw new NotFoundException($"User '{username}' not found.");
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddRoleToUserAsync(string username, AddUserRoleRequest request)
        {
            var normalizedUsername = username.Normalize();
            var user = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Username.Normalize() == normalizedUsername);
            if (user == null)
                throw new NotFoundException($"User '{username}' not found.");
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName);
            if (role == null)
            {
                role = new Role { Name = request.RoleName };
                _dbContext.Roles.Add(role);
                await _dbContext.SaveChangesAsync();
            }
            if (!user.Roles.Contains(role))
            {
                user.Roles.Add(role);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
