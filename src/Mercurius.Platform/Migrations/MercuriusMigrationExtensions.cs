using Microsoft.EntityFrameworkCore;

namespace Mercurius.Platform.Migrations;

public static class MercuriusMigrationExtensions
{
    public static WebApplication ApplyMercuriusMigrations<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        dbContext.Database.Migrate();

        return app;
    }
}
