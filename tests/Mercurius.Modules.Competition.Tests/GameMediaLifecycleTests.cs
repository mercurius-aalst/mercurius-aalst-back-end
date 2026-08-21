using Mercurius.LAN.API.Data;
using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Media.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Platform.Eventing;

namespace Mercurius.Modules.Competition.Tests;

public sealed class GameMediaLifecycleTests
{
    private const string PreviousImageUrl = "images/previous-game.webp";
    private const string NewImageUrl = "images/new-game.webp";

    [Fact]
    public async Task CreateGameAsync_WhenPersistenceFails_CompensatesOnlyNewImageAndPreservesOriginalFailure()
    {
        await using var dbContext = CreateDbContext();
        var persistenceFailure = new DbUpdateException("Game persistence failed.");
        var cleanupFailure = new IOException("Image cleanup failed.");
        var competitionDbContext = new InstrumentedCompetitionDbContext(
            dbContext,
            _ => Task.FromException<int>(persistenceFailure));
        var mediaModule = new RecordingMediaModule(NewImageUrl, cleanupFailure);
        var logger = new RecordingLogger<GameService>();
        var service = CreateService(competitionDbContext, mediaModule, logger: logger);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.CreateGameAsync(CreateGameDto()));

        Assert.Same(persistenceFailure, exception);
        var cleanup = Assert.Single(mediaModule.DeleteCalls);
        Assert.Equal(NewImageUrl, cleanup.ImageUrl);
        Assert.Equal(CancellationToken.None, cleanup.CancellationToken);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning &&
            ReferenceEquals(entry.Exception, cleanupFailure));
        Assert.False(await dbContext.Set<Game>().AnyAsync());
    }

    [Fact]
    public async Task CreateGameAsync_WhenPersistenceIsCanceled_CompensatesWithNonCanceledTokenAndPreservesCancellation()
    {
        await using var dbContext = CreateDbContext();
        using var cancellationSource = new CancellationTokenSource();
        var cancellation = new OperationCanceledException(
            "Game persistence was canceled.",
            cancellationSource.Token);
        var competitionDbContext = new InstrumentedCompetitionDbContext(
            dbContext,
            _ =>
            {
                cancellationSource.Cancel();
                return Task.FromException<int>(cancellation);
            });
        var mediaModule = new RecordingMediaModule(NewImageUrl);
        var service = CreateService(competitionDbContext, mediaModule);

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.CreateGameAsync(CreateGameDto(), cancellationSource.Token));

        Assert.Same(cancellation, exception);
        var cleanup = Assert.Single(mediaModule.DeleteCalls);
        Assert.Equal(NewImageUrl, cleanup.ImageUrl);
        Assert.Equal(CancellationToken.None, cleanup.CancellationToken);
        Assert.False(cleanup.CancellationToken.CanBeCanceled);
        Assert.False(await dbContext.Set<Game>().AnyAsync());
    }

    [Theory]
    [InlineData(FailureStage.OutboxStaging)]
    [InlineData(FailureStage.Persistence)]
    public async Task UpdateGameAsync_WhenCommitFails_CompensatesNewImageButNeverPreviousImage(
        FailureStage failureStage)
    {
        await using var dbContext = CreateDbContext();
        var game = CreateStoredGame();
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var commitFailure = new InvalidOperationException($"{failureStage} failed.");
        var competitionDbContext = new InstrumentedCompetitionDbContext(
            dbContext,
            failureStage == FailureStage.Persistence
                ? _ => Task.FromException<int>(commitFailure)
                : null);
        IModuleEventPublisher publisher = failureStage == FailureStage.OutboxStaging
            ? new ThrowingModuleEventPublisher(commitFailure)
            : CompetitionTestSupport.CreateModuleEventPublisher();
        var mediaModule = new RecordingMediaModule(NewImageUrl);
        var service = CreateService(competitionDbContext, mediaModule, publisher);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateGameAsync(game.Id, CreateUpdateDto()));

        Assert.Same(commitFailure, exception);
        var cleanup = Assert.Single(mediaModule.DeleteCalls);
        Assert.Equal(NewImageUrl, cleanup.ImageUrl);
        Assert.Equal(CancellationToken.None, cleanup.CancellationToken);
        Assert.DoesNotContain(mediaModule.DeleteCalls, call => call.ImageUrl == PreviousImageUrl);
        Assert.Equal(
            failureStage == FailureStage.OutboxStaging ? 0 : 1,
            competitionDbContext.SaveChangesCallCount);

        dbContext.ChangeTracker.Clear();
        var storedGame = await dbContext.Set<Game>().SingleAsync(candidate => candidate.Id == game.Id);
        Assert.Equal(PreviousImageUrl, storedGame.ImageUrl);
    }

    [Fact]
    public async Task UpdateGameAsync_WhenReplacementCommits_RetiresPreviousImageAfterPersistence()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateStoredGame();
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var operations = new List<string>();
        var competitionDbContext = new InstrumentedCompetitionDbContext(dbContext, operations: operations);
        var mediaModule = new RecordingMediaModule(NewImageUrl, operations: operations);
        var service = CreateService(competitionDbContext, mediaModule);

        var result = await service.UpdateGameAsync(game.Id, CreateUpdateDto());

        Assert.Equal(NewImageUrl, result.ImageUrl);
        Assert.Equal(
            ["persistence-complete", $"delete:{PreviousImageUrl}"],
            operations);
        var cleanup = Assert.Single(mediaModule.DeleteCalls);
        Assert.Equal(PreviousImageUrl, cleanup.ImageUrl);
        Assert.Equal(CancellationToken.None, cleanup.CancellationToken);
    }

    [Fact]
    public async Task DeleteGameAsync_WhenDeletionCommits_RetiresCurrentImageAfterPersistence()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateStoredGame();
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var operations = new List<string>();
        var competitionDbContext = new InstrumentedCompetitionDbContext(dbContext, operations: operations);
        var mediaModule = new RecordingMediaModule(NewImageUrl, operations: operations);
        var service = CreateService(competitionDbContext, mediaModule);

        await service.DeleteGameAsync(game.Id);

        Assert.Equal(
            ["persistence-complete", $"delete:{PreviousImageUrl}"],
            operations);
        var cleanup = Assert.Single(mediaModule.DeleteCalls);
        Assert.Equal(PreviousImageUrl, cleanup.ImageUrl);
        Assert.Equal(CancellationToken.None, cleanup.CancellationToken);
        Assert.False(await dbContext.Set<Game>().AnyAsync(candidate => candidate.Id == game.Id));
    }

    [Fact]
    public async Task DeleteGameAsync_WhenPersistenceFails_NeverDeletesCurrentImage()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateStoredGame();
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var persistenceFailure = new DbUpdateException("Game deletion failed.");
        var competitionDbContext = new InstrumentedCompetitionDbContext(
            dbContext,
            _ => Task.FromException<int>(persistenceFailure));
        var mediaModule = new RecordingMediaModule(NewImageUrl);
        var service = CreateService(competitionDbContext, mediaModule);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.DeleteGameAsync(game.Id));

        Assert.Same(persistenceFailure, exception);
        Assert.Empty(mediaModule.DeleteCalls);
        dbContext.ChangeTracker.Clear();
        Assert.True(await dbContext.Set<Game>().AnyAsync(candidate => candidate.Id == game.Id));
    }

    [Fact]
    public async Task UpdateGameAsync_WhenPostCommitCleanupFails_LogsWarningAndReturnsCommittedReplacement()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateStoredGame();
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var cleanupFailure = new IOException("Previous image could not be deleted.");
        var mediaModule = new RecordingMediaModule(NewImageUrl, cleanupFailure);
        var logger = new RecordingLogger<GameService>();
        var service = CreateService(
            new InstrumentedCompetitionDbContext(dbContext),
            mediaModule,
            logger: logger);

        var result = await service.UpdateGameAsync(game.Id, CreateUpdateDto());

        Assert.Equal(NewImageUrl, result.ImageUrl);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning &&
            ReferenceEquals(entry.Exception, cleanupFailure));
        dbContext.ChangeTracker.Clear();
        Assert.Equal(
            NewImageUrl,
            (await dbContext.Set<Game>().SingleAsync(candidate => candidate.Id == game.Id)).ImageUrl);
    }

    [Fact]
    public async Task DeleteGameAsync_WhenPostCommitCleanupFails_LogsWarningAndLeavesGameDeleted()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateStoredGame();
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var cleanupFailure = new IOException("Current image could not be deleted.");
        var mediaModule = new RecordingMediaModule(NewImageUrl, cleanupFailure);
        var logger = new RecordingLogger<GameService>();
        var service = CreateService(
            new InstrumentedCompetitionDbContext(dbContext),
            mediaModule,
            logger: logger);

        await service.DeleteGameAsync(game.Id);

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning &&
            ReferenceEquals(entry.Exception, cleanupFailure));
        Assert.False(await dbContext.Set<Game>().AnyAsync(candidate => candidate.Id == game.Id));
    }

    private static GameService CreateService(
        ICompetitionDbContext dbContext,
        IMediaModule mediaModule,
        IModuleEventPublisher? moduleEventPublisher = null,
        ILogger<GameService>? logger = null)
    {
        return new GameService(
            dbContext,
            new UnsupportedMatchModeratorFactory(),
            mediaModule,
            CompetitionTestSupport.CreateSponsorshipModule(),
            CompetitionTestSupport.CreateMapper(),
            moduleEventPublisher ?? CompetitionTestSupport.CreateModuleEventPublisher(),
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<GameService>.Instance);
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static Game CreateStoredGame()
    {
        return new Game(
            "Lifecycle Cup",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf3,
            ParticipationMode.Individual)
        {
            Id = Guid.NewGuid(),
            ImageUrl = PreviousImageUrl
        };
    }

    private static CreateGameDTO CreateGameDto()
    {
        return new CreateGameDTO
        {
            Name = "Lifecycle Cup",
            BracketType = Mercurius.Modules.Competition.Contracts.BracketType.SingleElimination,
            Format = Mercurius.Modules.Competition.Contracts.GameFormat.BestOf1,
            FinalsFormat = Mercurius.Modules.Competition.Contracts.GameFormat.BestOf3,
            ParticipationMode = Mercurius.Modules.Competition.Contracts.ParticipationMode.Individual,
            Image = CreateImage(),
            PlannedStartTime = DateTime.UtcNow.AddHours(1),
            AverageGameDurationMinutes = 30,
            RoundBreakDurationMinutes = 10
        };
    }

    private static UpdateGameDTO CreateUpdateDto()
    {
        return new UpdateGameDTO
        {
            Name = "Updated Lifecycle Cup",
            BracketType = Mercurius.Modules.Competition.Contracts.BracketType.SingleElimination,
            Format = Mercurius.Modules.Competition.Contracts.GameFormat.BestOf1,
            FinalsFormat = Mercurius.Modules.Competition.Contracts.GameFormat.BestOf3,
            ParticipationMode = Mercurius.Modules.Competition.Contracts.ParticipationMode.Individual,
            Image = CreateImage(),
            PlannedStartTime = DateTime.UtcNow.AddHours(2),
            AverageGameDurationMinutes = 30,
            RoundBreakDurationMinutes = 10
        };
    }

    private static FormFile CreateImage()
    {
        var bytes = new byte[] { 1, 2, 3 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "image", "game.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    public enum FailureStage
    {
        OutboxStaging,
        Persistence
    }

    private sealed class InstrumentedCompetitionDbContext : ICompetitionDbContext
    {
        private readonly MercuriusDBContext _inner;
        private readonly Func<CancellationToken, Task<int>>? _saveChanges;
        private readonly List<string>? _operations;

        public InstrumentedCompetitionDbContext(
            MercuriusDBContext inner,
            Func<CancellationToken, Task<int>>? saveChanges = null,
            List<string>? operations = null)
        {
            _inner = inner;
            _saveChanges = saveChanges;
            _operations = operations;
        }

        public DbSet<Game> Games => _inner.Set<Game>();
        public DbSet<Match> Matches => _inner.Set<Match>();
        public DbSet<Placement> Placements => _inner.Set<Placement>();
        public DbSet<TournamentRegistration> TournamentRegistrations => _inner.Set<TournamentRegistration>();
        public DbSet<TournamentRegistrationRosterMember> TournamentRegistrationRosterMembers =>
            _inner.Set<TournamentRegistrationRosterMember>();
        public DatabaseFacade Database => _inner.Database;
        public int SaveChangesCallCount { get; private set; }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            var result = _saveChanges is null
                ? await _inner.SaveChangesAsync(cancellationToken)
                : await _saveChanges(cancellationToken);
            _operations?.Add("persistence-complete");
            return result;
        }
    }

    private sealed class RecordingMediaModule : IMediaModule
    {
        private readonly string _savedUrl;
        private readonly Exception? _deleteFailure;
        private readonly List<string>? _operations;

        public RecordingMediaModule(
            string savedUrl,
            Exception? deleteFailure = null,
            List<string>? operations = null)
        {
            _savedUrl = savedUrl;
            _deleteFailure = deleteFailure;
            _operations = operations;
        }

        public List<DeleteCall> DeleteCalls { get; } = [];

        public Task<StoredMediaAsset> SaveImageAsync(
            MediaUpload upload,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StoredMediaAsset(_savedUrl));
        }

        public Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(new DeleteCall(imageUrl, cancellationToken));
            _operations?.Add($"delete:{imageUrl}");
            return _deleteFailure is null
                ? Task.CompletedTask
                : Task.FromException(_deleteFailure);
        }
    }

    private sealed record DeleteCall(string? ImageUrl, CancellationToken CancellationToken);

    private sealed class ThrowingModuleEventPublisher(Exception exception) : IModuleEventPublisher
    {
        public Guid Publish<TPayload>(TPayload payload, DateTime? occurredAtUtc = null)
            where TPayload : notnull
        {
            throw exception;
        }
    }

    private sealed class UnsupportedMatchModeratorFactory : IMatchModeratorFactory
    {
        public IMatchModerator GetMatchModerator(BracketType bracketType)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);
}
