using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mercurius.LAN.API.Tests;

public sealed class ModulePublicSurfaceTests
{
    private static readonly IReadOnlyDictionary<string, string[]> ExpectedPublicTypes =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Competition"] = ["Mercurius.Modules.Competition.CompetitionModuleConfiguration"],
            ["Discovery"] = ["Mercurius.Modules.Discovery.DiscoveryModuleConfiguration"],
            ["Identity"] =
            [
                "Mercurius.Modules.Identity.IdentityModuleConfiguration",
                "Mercurius.Modules.Identity.Domain.User",
                "Mercurius.Modules.Identity.Infrastructure.IIdentityDbContext"
            ],
            ["Media"] = ["Mercurius.Modules.Media.MediaModuleConfiguration"],
            ["Sponsorship"] = ["Mercurius.Modules.Sponsorship.SponsorshipModuleConfiguration"],
            ["Teams"] =
            [
                "Mercurius.Modules.Teams.TeamsModuleConfiguration",
                "Mercurius.Modules.Teams.Domain.Team"
            ]
        };

    public static IEnumerable<object[]> Modules => ExpectedPublicTypes.Keys.Select(moduleName => new object[] { moduleName });

    [Theory]
    [MemberData(nameof(Modules))]
    public void ModuleImplementation_ExportsOnlyCompositionRootsAndDocumentedTemporaryPersistenceTypes(string moduleName)
    {
        var assembly = Assembly.Load($"Mercurius.Modules.{moduleName}");
        var exportedTypeNames = assembly
            .GetExportedTypes()
            .Select(type => type.FullName ?? type.Name)
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();
        var expectedTypeNames = ExpectedPublicTypes[moduleName]
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedTypeNames, exportedTypeNames);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void ModuleImplementation_GrantsInternalAccessOnlyToTestAssemblies(string moduleName)
    {
        var assembly = Assembly.Load($"Mercurius.Modules.{moduleName}");
        var friendAssemblyNames = assembly
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(attribute => new AssemblyName(attribute.AssemblyName).Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!);

        Assert.All(
            friendAssemblyNames,
            name => Assert.True(name.EndsWith(".Tests", StringComparison.Ordinal), $"{name} is not a test assembly."));
    }
}
