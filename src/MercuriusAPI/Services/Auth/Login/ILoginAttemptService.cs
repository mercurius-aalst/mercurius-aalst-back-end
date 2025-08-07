using MercuriusAPI.DTOs.Auth;
using System.Threading.Tasks;

namespace MercuriusAPI.Services.Auth.Login
{
    public interface ILoginAttemptService
    {
        bool IsLockedOut(string username, System.DateTime now);
        int RegisterFailedAttempt(string username, System.DateTime now);
        void Reset(string username);
    }
}
