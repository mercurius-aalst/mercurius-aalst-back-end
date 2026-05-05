using System.Collections.Concurrent;

namespace Auth.Module.Services.External;

public class OidcStateStore : IOidcStateStore
{
    private readonly ConcurrentDictionary<string, OidcStateEntry> _entries = new();

    public Task StoreAsync(OidcStateEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries[entry.State] = entry;
        return Task.CompletedTask;
    }

    public Task<OidcStateEntry?> TakeAsync(string state, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(state))
            return Task.FromResult<OidcStateEntry?>(null);

        if (!_entries.TryRemove(state, out var entry))
            return Task.FromResult<OidcStateEntry?>(null);

        if (entry.ExpiresAtUtc <= DateTime.UtcNow)
            return Task.FromResult<OidcStateEntry?>(null);

        return Task.FromResult<OidcStateEntry?>(entry);
    }
}
