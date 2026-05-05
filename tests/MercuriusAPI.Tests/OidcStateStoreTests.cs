using Auth.Module.Services.External;

namespace Mercurius.LAN.API.Tests;

public class OidcStateStoreTests
{
    [Fact]
    public async Task TakeAsync_ReturnsStoredEntry_Once()
    {
        var store = new OidcStateStore();
        var entry = new OidcStateEntry
        {
            State = "state-1",
            Nonce = "nonce-1",
            CodeVerifier = "verifier-1",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        };

        await store.StoreAsync(entry);

        var firstRead = await store.TakeAsync(entry.State);
        var secondRead = await store.TakeAsync(entry.State);

        Assert.NotNull(firstRead);
        Assert.Null(secondRead);
    }

    [Fact]
    public async Task TakeAsync_ReturnsNull_ForExpiredEntry()
    {
        var store = new OidcStateStore();
        await store.StoreAsync(new OidcStateEntry
        {
            State = "expired",
            Nonce = "nonce",
            CodeVerifier = "verifier",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });

        var result = await store.TakeAsync("expired");

        Assert.Null(result);
    }
}
