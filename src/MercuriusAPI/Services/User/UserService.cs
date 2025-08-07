using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Data;
using MercuriusAPI.Models;
using MercuriusAPI.Models.Auth;
using Microsoft.EntityFrameworkCore;
using MercuriusAPI.Exceptions;
using System.Threading.Tasks;
using System.Linq;

namespace MercuriusAPI.Services.User
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
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                throw new NotFoundException($"User '{username}' not found.");
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddRoleToUserAsync(AddUserRoleRequest request)
        {
            var user = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
                throw new NotFoundException($"User '{request.Username}' not found.");
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
