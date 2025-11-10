using System.Collections.Concurrent;

namespace MercuriusAPI.Services.Auth.Login;

public class LoginAttemptService : ILoginAttemptService
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _lockoutDuration;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, (int Attempts, DateTime? LockoutEnd, DateTime? FirstAttempt)> _attempts = new();

    public LoginAttemptService(int maxAttempts, TimeSpan window, TimeSpan lockoutDuration)
    {
        _maxAttempts = maxAttempts;
        _window = window;
        _lockoutDuration = lockoutDuration;
    }

    public bool IsLockedOut(string username, DateTime now)
    {
        if (_attempts.TryGetValue(username, out var entry) && entry.LockoutEnd.HasValue)
            return entry.LockoutEnd > now;
        return false;
    }

    public int RegisterFailedAttempt(string username, DateTime now)
    {
        _attempts.AddOrUpdate(username,
            _ => (1, null, now),
            (_, entry) =>
            {
                if (entry.LockoutEnd.HasValue && entry.LockoutEnd > now)
                    return entry;
                if (entry.FirstAttempt.HasValue && now - entry.FirstAttempt.Value > _window)
                    return (1, null, now);
                var attempts = entry.Attempts + 1;
                if (attempts >= _maxAttempts)
                    return (attempts, now.Add(_lockoutDuration), entry.FirstAttempt);
                return (attempts, null, entry.FirstAttempt ?? now);
            });
        return _attempts[username].Attempts >= _maxAttempts ? 0 : _maxAttempts - _attempts[username].Attempts;
    }

    public void Reset(string username)
    {
        _attempts.TryRemove(username, out _);
    }
}
