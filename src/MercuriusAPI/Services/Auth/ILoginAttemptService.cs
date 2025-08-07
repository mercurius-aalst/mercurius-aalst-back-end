using System;

namespace MercuriusAPI.Services.Auth
{
    public interface ILoginAttemptService
    {
        bool IsLockedOut(string username, DateTime now);
        int RegisterFailedAttempt(string username, DateTime now);
        void Reset(string username);
    }
}
