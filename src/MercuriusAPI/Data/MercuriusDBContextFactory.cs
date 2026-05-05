using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Mercurius.LAN.API.Data;

public sealed class MercuriusDBContextFactory : IDesignTimeDbContextFactory<MercuriusDBContext>
{
    public MercuriusDBContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables("Mercurius.LAN.API_")
            .Build();

        var connectionString = configuration.GetConnectionString("MercuriusDB")
            ?? throw new InvalidOperationException("Connection string 'MercuriusDB' was not found.");

        var optionsBuilder = new DbContextOptionsBuilder<MercuriusDBContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MercuriusDBContext(optionsBuilder.Options);
    }
}
