using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mercurius.Modules.Competition.Application.Services;

internal sealed class TournamentRegistrationPersistenceCoordinator(ICompetitionDbContext dbContext)
{
    public async Task SaveChangesAsync(string duplicateMessage, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsRegistrationUniqueConstraintViolation(exception))
        {
            throw new ValidationException(duplicateMessage);
        }
    }

    public async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return null;

        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private static bool IsRegistrationUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("IX_TournamentRegistrations_", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("IX_TournamentRosterMembers_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
