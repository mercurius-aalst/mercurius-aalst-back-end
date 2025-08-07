using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MercuriusAPI.Services.Auth
{
    public class LoginAttemptService : ILoginAttemptService
    {
        private readonly int _maxFailedAttempts;
        private readonly TimeSpan _lockoutDuration;
        private readonly TimeSpan _failedAttemptWindow;
        private readonly ConcurrentDictionary<string, List<DateTime>> _failedLoginAttempts = new();
        private readonly ConcurrentDictionary<string, DateTime?> _lockoutEnd = new();

        public LoginAttemptService(int maxFailedAttempts, TimeSpan lockoutDuration, TimeSpan failedAttemptWindow)
        {
            _maxFailedAttempts = maxFailedAttempts;
            _lockoutDuration = lockoutDuration;
            _failedAttemptWindow = failedAttemptWindow;
        }

        public bool IsLockedOut(string username, DateTime now)
        {
            return _lockoutEnd.TryGetValue(username, out var lockoutUntil) && lockoutUntil.HasValue && lockoutUntil.Value > now;
        }

        public int RegisterFailedAttempt(string username, DateTime now)
        {
            var attempts = _failedLoginAttempts.GetOrAdd(username, _ => new List<DateTime>());
            attempts.RemoveAll(dt => dt < now - _failedAttemptWindow);
            attempts.Add(now);
            if (attempts.Count >= _maxFailedAttempts)
            {
                _lockoutEnd[username] = now.Add(_lockoutDuration);
                _failedLoginAttempts[username] = new List<DateTime>();
                return 0;
            }
            return _maxFailedAttempts - attempts.Count;
        }

        public void Reset(string username)
        {
            _failedLoginAttempts[username] = new List<DateTime>();
            _lockoutEnd[username] = null;
        }
    }
}
