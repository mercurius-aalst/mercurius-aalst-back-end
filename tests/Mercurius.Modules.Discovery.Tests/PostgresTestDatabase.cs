using Npgsql;
using Mercurius.LAN.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Discovery.Tests;

internal static class PostgresTestDatabase
{
    public static PostgresTestDatabaseLease Create()
    {
        var databaseName = $"mercurius_tests_{Guid.NewGuid():N}";
        var adminBuilder = new NpgsqlConnectionStringBuilder(GetBaseConnectionString())
        {
            Database = "postgres"
        };

        using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        command.ExecuteNonQuery();

        var databaseBuilder = new NpgsqlConnectionStringBuilder(adminBuilder.ConnectionString)
        {
            Database = databaseName
        };

        return new PostgresTestDatabaseLease(
            databaseName,
            adminBuilder.ConnectionString,
            databaseBuilder.ConnectionString);
    }

    public static void Initialize(MercuriusDBContext dbContext)
    {
        dbContext.Database.Migrate();
    }

    private static string GetBaseConnectionString() =>
        Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
        ?? "Host=localhost;Port=5432;Username=postgres;Password=postgres;Timeout=5;Command Timeout=30";
}

internal sealed class PostgresTestDatabaseLease : IDisposable, IAsyncDisposable
{
    private readonly string _adminConnectionString;
    private readonly string _databaseName;
    private int _disposed;

    internal PostgresTestDatabaseLease(
        string databaseName,
        string adminConnectionString,
        string connectionString)
    {
        _databaseName = databaseName;
        _adminConnectionString = adminConnectionString;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
        GC.SuppressFinalize(this);
    }
}
