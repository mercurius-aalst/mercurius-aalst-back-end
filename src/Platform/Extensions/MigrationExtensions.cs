using Microsoft.EntityFrameworkCore;

namespace Platform.Extensions;

public static class MigrationExtensions
{
    public static WebApplication ApplyMigrations<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        dbContext.Database.Migrate();

        return app;
    }
}
