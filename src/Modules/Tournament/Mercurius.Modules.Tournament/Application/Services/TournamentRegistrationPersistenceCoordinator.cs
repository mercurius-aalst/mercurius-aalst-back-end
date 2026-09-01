using Mercurius.Modules.Tournament.Infrastructure;
using Mercurius.Modules.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class TournamentRegistrationPersistenceCoordinator(ITournamentDbContext dbContext)
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

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        dbContext.Database.BeginTransactionAsync(cancellationToken);

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
