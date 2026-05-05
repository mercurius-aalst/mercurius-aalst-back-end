namespace Auth.Module.Services.External;

public interface IOidcStateStore
{
    Task StoreAsync(OidcStateEntry entry, CancellationToken cancellationToken = default);
    Task<OidcStateEntry?> TakeAsync(string state, CancellationToken cancellationToken = default);
}
