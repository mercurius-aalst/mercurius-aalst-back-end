using Mercurius.LAN.API.Data;
using Mercurius.Modules.Discovery.Application;
using Mercurius.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Mercurius.Modules.Discovery.Tests;

public sealed class DiscoveryRebuildOwnershipTests
{
    private const long RebuildLockKey = unchecked((long)0x4D45524355524955UL);

    [Fact]
    public async Task RebuildOwnership_ContendsOnOneSessionAndRecoversAfterSessionLoss()
    {
        var sources = new DiscoveryModuleTests.DiscoverySources
        {
            Users = Enumerable.Range(1, 1001)
                .Select(index => new Mercurius.Modules.Identity.Contracts.PublicUserSearchDocument(
                    new Mercurius.Modules.Shared.UserId(Guid.NewGuid()),
                    $"ownership-user-{index:D4}"))
                .ToArray(),
            PauseAfterFirstUserPage = true
        };
        await using var provider = DiscoveryModuleTests.CreateProvider(sources);
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        await InstallBackendPidCaptureAsync(firstDb);

        var firstOwner = firstScope.ServiceProvider.GetRequiredService<DiscoveryRebuildOwnership>();
        var secondOwner = secondScope.ServiceProvider.GetRequiredService<DiscoveryRebuildOwnership>();
        var firstService = firstScope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();
        var createModule = firstScope.ServiceProvider.GetRequiredService<Contracts.IDiscoveryModule>();
        var job = await createModule.CreateSearchIndexRebuildJobAsync();

        await using var firstLease = Assert.IsType<DiscoveryRebuildOwnership.Lease>(await firstOwner.TryAcquireAsync(default));
        var firstBackendPid = await ReadBackendPidAsync(firstDb);
        var runTask = firstService.RunNextAsync(default);
        await sources.RebuildStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Null(await secondOwner.TryAcquireAsync(default));
        await Assert.ThrowsAsync<DiscoveryRebuildOwnershipLostException>(
            () => secondService.RecoverInterruptedJobsAsync(default));
        Assert.Equal(SearchIndexRebuildJobStatus.Running, await secondDb.Set<SearchIndexRebuildJob>()
            .Where(candidate => candidate.Id == job.Id)
            .Select(candidate => candidate.Status)
            .SingleAsync());
        Assert.Equal(1000, await secondDb.Set<SearchIndexRebuildDocument>().CountAsync());

        await TerminateBackendAsync(firstDb.Database.GetDbConnection().ConnectionString, firstBackendPid);
        sources.ContinueRebuild.TrySetResult();
        await Assert.ThrowsAsync<DiscoveryRebuildOwnershipLostException>(() => runTask);
        await firstLease.DisposeAsync();

        Assert.Equal(SearchIndexRebuildJobStatus.Running, await secondDb.Set<SearchIndexRebuildJob>()
            .Where(candidate => candidate.Id == job.Id)
            .Select(candidate => candidate.Status)
            .SingleAsync());
        Assert.Equal(1000, await secondDb.Set<SearchIndexRebuildDocument>().CountAsync());

        sources.PauseAfterFirstUserPage = false;
        var secondLease = Assert.IsType<DiscoveryRebuildOwnership.Lease>(await secondOwner.TryAcquireAsync(default));
        try
        {
            var secondBackendPid = await ReadBackendPidAsync(secondDb);
            await secondService.RecoverInterruptedJobsAsync(default);
            Assert.Equal(SearchIndexRebuildJobStatus.Pending, await secondDb.Set<SearchIndexRebuildJob>()
                .Where(candidate => candidate.Id == job.Id)
                .Select(candidate => candidate.Status)
                .SingleAsync());
            Assert.Empty(await secondDb.Set<SearchIndexRebuildDocument>().ToListAsync());

            Assert.True(await secondService.RunNextAsync(default));
            Assert.Equal(SearchIndexRebuildJobStatus.Completed, await secondDb.Set<SearchIndexRebuildJob>()
                .Where(candidate => candidate.Id == job.Id)
                .Select(candidate => candidate.Status)
                .SingleAsync());
            Assert.Equal(1001, await secondDb.Set<SearchDocument>().CountAsync(document => !document.IsDeleted));
            Assert.Empty(await secondDb.Set<SearchIndexRebuildDocument>().ToListAsync());

            var writerBackendPids = await secondDb.Database
                .SqlQuery<int>($"SELECT DISTINCT backend_pid AS \"Value\" FROM discovery.rebuild_backend_pids")
                .ToListAsync();
            Assert.Equal(new[] { firstBackendPid, secondBackendPid }.Order().ToArray(), writerBackendPids.Order().ToArray());
        }
        finally
        {
            await secondLease.DisposeAsync();
        }
    }

    [Fact]
    public async Task RebuildOwnership_LostSessionDuringMergeRollsBackBeforeNextOwnerRecovers()
    {
        var sources = new DiscoveryModuleTests.DiscoverySources
        {
            Users = [new Mercurius.Modules.Identity.Contracts.PublicUserSearchDocument(
                new Mercurius.Modules.Shared.UserId(Guid.NewGuid()),
                "merge-owner-user")]
        };
        await using var provider = DiscoveryModuleTests.CreateProvider(sources);
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        await InstallBackendPidCaptureAsync(firstDb);
        await InstallMergePauseAsync(firstDb);

        var firstOwner = firstScope.ServiceProvider.GetRequiredService<DiscoveryRebuildOwnership>();
        var secondOwner = secondScope.ServiceProvider.GetRequiredService<DiscoveryRebuildOwnership>();
        var firstService = firstScope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();
        var secondService = secondScope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();
        var createModule = firstScope.ServiceProvider.GetRequiredService<Contracts.IDiscoveryModule>();
        var job = await createModule.CreateSearchIndexRebuildJobAsync();

        await using var firstLease = Assert.IsType<DiscoveryRebuildOwnership.Lease>(await firstOwner.TryAcquireAsync(default));
        var firstBackendPid = await ReadBackendPidAsync(firstDb);
        var runTask = firstService.RunNextAsync(default);
        var connectionString = firstDb.Database.GetDbConnection().ConnectionString;
        await WaitForMergeAsync(connectionString, firstBackendPid);
        await TerminateBackendAsync(connectionString, firstBackendPid);
        await Assert.ThrowsAsync<DiscoveryRebuildOwnershipLostException>(() => runTask);

        Assert.Equal(SearchIndexRebuildJobStatus.Running, await secondDb.Set<SearchIndexRebuildJob>()
            .Where(candidate => candidate.Id == job.Id)
            .Select(candidate => candidate.Status)
            .SingleAsync());
        Assert.Empty(await secondDb.Set<SearchDocument>().ToListAsync());
        Assert.Single(await secondDb.Set<SearchIndexRebuildDocument>().ToListAsync());

        await firstLease.DisposeAsync();
        await RemoveMergePauseAsync(secondDb);
        var secondLease = Assert.IsType<DiscoveryRebuildOwnership.Lease>(await secondOwner.TryAcquireAsync(default));
        try
        {
            var secondBackendPid = await ReadBackendPidAsync(secondDb);
            await secondService.RecoverInterruptedJobsAsync(default);
            Assert.Empty(await secondDb.Set<SearchIndexRebuildDocument>().ToListAsync());
            Assert.True(await secondService.RunNextAsync(default));
            Assert.Equal(SearchIndexRebuildJobStatus.Completed, await secondDb.Set<SearchIndexRebuildJob>()
                .Where(candidate => candidate.Id == job.Id)
                .Select(candidate => candidate.Status)
                .SingleAsync());
            Assert.Equal("merge-owner-user", (await secondDb.Set<SearchDocument>().SingleAsync()).Title);

            var writerBackendPids = await secondDb.Database
                .SqlQuery<int>($"SELECT DISTINCT backend_pid AS \"Value\" FROM discovery.rebuild_backend_pids")
                .ToListAsync();
            Assert.Equal(new[] { firstBackendPid, secondBackendPid }.Order().ToArray(), writerBackendPids.Order().ToArray());
        }
        finally
        {
            await secondLease.DisposeAsync();
        }
    }

    [Fact]
    public async Task HostedWorkers_AlreadyPollingWorkerRecoversAfterOwnerSessionLoss()
    {
        var sources = new DiscoveryModuleTests.DiscoverySources
        {
            Users = Enumerable.Range(1, 1001)
                .Select(index => new Mercurius.Modules.Identity.Contracts.PublicUserSearchDocument(
                    new Mercurius.Modules.Shared.UserId(Guid.NewGuid()),
                    $"hosted-owner-user-{index:D4}"))
                .ToArray(),
            PauseAfterFirstUserPage = true
        };
        await using var database = Mercurius.TestInfrastructure.PostgresTestDatabase.Create();
        var firstConnectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString)
        {
            ApplicationName = "discovery-rebuild-owner-a"
        }.ConnectionString;
        var secondConnectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString)
        {
            ApplicationName = "discovery-rebuild-owner-b"
        }.ConnectionString;
        await using (var migrationContext = CreateDbContext(database.ConnectionString))
        {
            await migrationContext.Database.MigrateAsync();
            await InstallBackendPidCaptureAsync(migrationContext);
        }

        using var firstHost = DiscoveryModuleTests.CreateHostForDatabase(firstConnectionString, sources);
        using var secondHost = DiscoveryModuleTests.CreateHostForDatabase(secondConnectionString, sources);
        await firstHost.StartAsync();
        await sources.RebuildStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var firstBackendPid = await WaitForSingleCapturedBackendPidAsync(database.ConnectionString);
        await secondHost.StartAsync();
        await Task.Delay(TimeSpan.FromSeconds(5));
        await using (var blockedScope = secondHost.Services.CreateAsyncScope())
        {
            var dbContext = blockedScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
            Assert.Equal(SearchIndexRebuildJobStatus.Running,
                await dbContext.Set<SearchIndexRebuildJob>().Select(job => job.Status).SingleAsync());
            Assert.Equal(1000, await dbContext.Set<SearchIndexRebuildDocument>().CountAsync());
        }

        await TerminateBackendAsync(database.ConnectionString, firstBackendPid);
        sources.PauseAfterFirstUserPage = false;
        sources.ContinueRebuild.TrySetResult();

        Assert.True(await WaitForAsync(async () =>
        {
            await using var scope = secondHost.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
            return await dbContext.Set<SearchIndexRebuildJob>()
                .AnyAsync(job => job.Status == SearchIndexRebuildJobStatus.Completed);
        }, TimeSpan.FromSeconds(30)), "The already-running second worker did not recover and complete the rebuild.");

        await using (var verificationScope = secondHost.Services.CreateAsyncScope())
        {
            var dbContext = verificationScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
            Assert.Equal(1001, await dbContext.Set<SearchDocument>().CountAsync(document => !document.IsDeleted));
            Assert.Empty(await dbContext.Set<SearchIndexRebuildDocument>().ToListAsync());
            var writerBackendPids = await dbContext.Database
                .SqlQuery<int>($"SELECT DISTINCT backend_pid AS \"Value\" FROM discovery.rebuild_backend_pids")
                .ToListAsync();
            Assert.Contains(firstBackendPid, writerBackendPids);
            Assert.Equal(2, writerBackendPids.Distinct().Count());
        }

        await firstHost.StopAsync();
        await secondHost.StopAsync();
    }

    [Fact]
    public async Task RebuildOwnership_NormalLeaseDisposalReleasesAdvisoryLockBeforeReturningConnectionToPool()
    {
        await using var provider = DiscoveryModuleTests.CreateProvider(new DiscoveryModuleTests.DiscoverySources());
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        var owner = scope.ServiceProvider.GetRequiredService<DiscoveryRebuildOwnership>();
        var lease = Assert.IsType<DiscoveryRebuildOwnership.Lease>(await owner.TryAcquireAsync(default));

        await lease.DisposeAsync();

        var connectionString = new NpgsqlConnectionStringBuilder(dbContext.Database.GetConnectionString())
        {
            Pooling = false
        }.ConnectionString;
        await using var independentConnection = new NpgsqlConnection(connectionString);
        await independentConnection.OpenAsync();
        await using var command = independentConnection.CreateCommand();
        command.CommandText = "SELECT pg_try_advisory_lock(@key);";
        command.Parameters.AddWithValue("key", RebuildLockKey);
        Assert.True(Convert.ToBoolean(await command.ExecuteScalarAsync()));

        command.CommandText = "SELECT pg_advisory_unlock(@key);";
        Assert.True(Convert.ToBoolean(await command.ExecuteScalarAsync()));
    }

    private static MercuriusDBContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new MercuriusDBContext(options);
    }

    private static async Task<int> WaitForSingleCapturedBackendPidAsync(string connectionString)
    {
        var timeoutAtUtc = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < timeoutAtUtc)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT DISTINCT backend_pid FROM discovery.rebuild_backend_pids;";
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return reader.GetInt32(0);

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new TimeoutException("The hosted rebuild did not write its first staging page.");
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var timeoutAtUtc = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < timeoutAtUtc)
        {
            if (await condition())
                return true;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return false;
    }

    private static async Task InstallBackendPidCaptureAsync(MercuriusDBContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync("CREATE TABLE discovery.rebuild_backend_pids (backend_pid integer NOT NULL);");
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION discovery.capture_rebuild_backend_pid() RETURNS trigger
            LANGUAGE plpgsql AS $$
            BEGIN
                INSERT INTO discovery.rebuild_backend_pids VALUES (pg_backend_pid());
                RETURN NEW;
            END;
            $$;
            """);
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER capture_rebuild_stage_pid
                BEFORE INSERT ON discovery.search_index_rebuild_documents
                FOR EACH ROW EXECUTE FUNCTION discovery.capture_rebuild_backend_pid();
            """);
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER capture_rebuild_live_pid
                BEFORE INSERT OR UPDATE ON discovery.search_documents
                FOR EACH ROW EXECUTE FUNCTION discovery.capture_rebuild_backend_pid();
            """);
    }

    private static async Task<int> ReadBackendPidAsync(MercuriusDBContext dbContext)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT pg_backend_pid();";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task InstallMergePauseAsync(MercuriusDBContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION discovery.pause_rebuild_merge() RETURNS trigger
            LANGUAGE plpgsql AS $$
            BEGIN
                PERFORM pg_sleep(30);
                RETURN NEW;
            END;
            $$;
            """);
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER pause_rebuild_merge
                BEFORE INSERT ON discovery.search_documents
                FOR EACH ROW EXECUTE FUNCTION discovery.pause_rebuild_merge();
            """);
    }

    private static async Task RemoveMergePauseAsync(MercuriusDBContext dbContext)
    {
        await dbContext.Database.ExecuteSqlRawAsync("DROP TRIGGER pause_rebuild_merge ON discovery.search_documents;");
        await dbContext.Database.ExecuteSqlRawAsync("DROP FUNCTION discovery.pause_rebuild_merge();");
    }

    private static async Task WaitForMergeAsync(string connectionString, int backendPid)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var timeoutAtUtc = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < timeoutAtUtc)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_stat_activity
                    WHERE pid = @pid
                      AND state = 'active'
                      AND query LIKE 'INSERT INTO discovery.search_documents%');
                """;
            command.Parameters.AddWithValue("pid", backendPid);
            if (Convert.ToBoolean(await command.ExecuteScalarAsync()))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new TimeoutException("The rebuild did not enter its merge transaction.");
    }

    private static async Task TerminateBackendAsync(string connectionString, int backendPid)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_terminate_backend(@pid);";
        command.Parameters.AddWithValue("pid", backendPid);
        Assert.True(Convert.ToBoolean(await command.ExecuteScalarAsync()));
    }
}
