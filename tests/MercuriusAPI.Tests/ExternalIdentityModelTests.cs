using Auth.Module.Models;
using Mercurius.LAN.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Tests;

public class ExternalIdentityModelTests
{
    [Fact]
    public void ExternalIdentity_HasUniqueProviderSubjectIndex()
    {
        using var dbContext = CreateDbContext();

        var entityType = dbContext.Model.FindEntityType(typeof(ExternalIdentity));

        Assert.NotNull(entityType);
        Assert.Contains(
            entityType!.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(property => property.Name).SequenceEqual([nameof(ExternalIdentity.Provider), nameof(ExternalIdentity.ProviderSubject)]));
    }

    [Fact]
    public void ExternalIdentity_HasUserIdIndex_AndRequiredUserRelationship()
    {
        using var dbContext = CreateDbContext();

        var entityType = dbContext.Model.FindEntityType(typeof(ExternalIdentity));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetIndexes(), index => index.Properties.Select(property => property.Name).SequenceEqual([nameof(ExternalIdentity.UserId)]));

        var foreignKey = Assert.Single(entityType.GetForeignKeys());
        Assert.Equal(nameof(ExternalIdentity.UserId), Assert.Single(foreignKey.Properties).Name);
        Assert.False(foreignKey.IsRequiredDependent);
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql("Host=localhost;Database=mercurius_tests;Username=test;Password=test")
            .Options;

        return new MercuriusDBContext(options);
    }
}
