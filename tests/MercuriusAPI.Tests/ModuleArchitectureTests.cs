using System.Reflection;
using System.Xml.Linq;
using Mercurius.Modules.Competition.Application.Services;
using Mercurius.Modules.Identity.Services;
using Platform.Eventing;

namespace Mercurius.LAN.API.Tests;

public class ModuleArchitectureTests
{
    private static readonly string[] ModuleNames =
    [
        "Competition",
        "Discovery",
        "Identity",
        "Media",
        "Sponsorship",
        "Teams"
    ];

    public static IEnumerable<object[]> Modules =>
        ModuleNames.Select(moduleName => new object[] { moduleName });

    [Fact]
    public void ApiHost_ReferencesEveryModuleImplementationAndSharedInfrastructure()
    {
        var repositoryRoot = FindRepositoryRoot();
        var apiProject = Path.Combine(repositoryRoot, "src", "MercuriusAPI", "Mercurius.LAN.API.csproj");
        var references = GetProjectReferences(apiProject);
        var expectedReferences = ModuleNames
            .Select(moduleName => Path.Combine(
                repositoryRoot,
                "src",
                "Modules",
                moduleName,
                $"Mercurius.Modules.{moduleName}",
                $"Mercurius.Modules.{moduleName}.csproj"))
            .Append(Path.Combine(repositoryRoot, "src", "Modules.Shared", "Modules.Shared.csproj"))
            .Append(Path.Combine(repositoryRoot, "src", "Platform", "Platform.csproj"));

        Assert.All(expectedReferences, expectedReference => Assert.Contains(expectedReference, references));
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void ModuleImplementation_ReferencesOnlyAllowedInfrastructure(string moduleName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var moduleDirectory = Path.Combine(repositoryRoot, "src", "Modules", moduleName);
        var implementationProject = Path.Combine(
            moduleDirectory,
            $"Mercurius.Modules.{moduleName}",
            $"Mercurius.Modules.{moduleName}.csproj");
        var references = GetProjectReferences(implementationProject);
        var requiredReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(
                moduleDirectory,
                $"Mercurius.Modules.{moduleName}.Contracts",
                $"Mercurius.Modules.{moduleName}.Contracts.csproj"),
            Path.Combine(repositoryRoot, "src", "Modules.Shared", "Modules.Shared.csproj")
        };
        var allowedReferences = new HashSet<string>(requiredReferences, StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(repositoryRoot, "src", "Platform", "Platform.csproj")
        };
        if (moduleName == "Teams")
        {
            allowedReferences.Add(Path.Combine(
                repositoryRoot,
                "src",
                "Modules",
                "Identity",
                "Mercurius.Modules.Identity",
                "Mercurius.Modules.Identity.csproj"));
        }
        else if (moduleName == "Competition")
        {
            allowedReferences.Add(Path.Combine(
                repositoryRoot,
                "src",
                "Modules",
                "Teams",
                "Mercurius.Modules.Teams.Contracts",
                "Mercurius.Modules.Teams.Contracts.csproj"));
            allowedReferences.Add(Path.Combine(
                repositoryRoot,
                "src",
                "Modules",
                "Identity",
                "Mercurius.Modules.Identity.Contracts",
                "Mercurius.Modules.Identity.Contracts.csproj"));
            allowedReferences.Add(Path.Combine(
                repositoryRoot,
                "src",
                "Modules",
                "Sponsorship",
                "Mercurius.Modules.Sponsorship.Contracts",
                "Mercurius.Modules.Sponsorship.Contracts.csproj"));
            allowedReferences.Add(Path.Combine(
                repositoryRoot,
                "src",
                "Modules",
                "Media",
                "Mercurius.Modules.Media.Contracts",
                "Mercurius.Modules.Media.Contracts.csproj"));
        }

        Assert.True(
            requiredReferences.IsSubsetOf(references) && references.IsSubsetOf(allowedReferences),
            $"Unexpected project references: {string.Join(", ", references)}");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void ModuleContracts_ReferenceOnlyModulesShared(string moduleName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var contractsProject = Path.Combine(
            repositoryRoot,
            "src",
            "Modules",
            moduleName,
            $"Mercurius.Modules.{moduleName}.Contracts",
            $"Mercurius.Modules.{moduleName}.Contracts.csproj");
        var references = GetProjectReferences(contractsProject);
        var expectedReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(repositoryRoot, "src", "Modules.Shared", "Modules.Shared.csproj")
        };

        Assert.True(
            expectedReferences.SetEquals(references),
            $"Unexpected project references: {string.Join(", ", references)}");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void ModuleContracts_ExposeModuleFacadeInterface(string moduleName)
    {
        var assembly = Assembly.Load($"Mercurius.Modules.{moduleName}.Contracts");
        var expectedInterfaceName = $"Mercurius.Modules.{moduleName}.Contracts.I{moduleName}Module";

        Assert.NotNull(assembly.GetType(expectedInterfaceName, throwOnError: false, ignoreCase: false));
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void ModuleContracts_DoNotExposeEntityFrameworkOrQueryableTypes(string moduleName)
    {
        var assembly = Assembly.Load($"Mercurius.Modules.{moduleName}.Contracts");
        var forbiddenTypes = assembly
            .GetExportedTypes()
            .SelectMany(GetPublicApiTypes)
            .Where(IsForbiddenContractType)
            .Select(type => type.FullName ?? type.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(typeName => typeName)
            .ToArray();

        Assert.True(
            forbiddenTypes.Length == 0,
            $"Contract assembly exposes forbidden types: {string.Join(", ", forbiddenTypes)}");
    }

    [Fact]
    public void ModulesShared_ExposesTypedIds()
    {
        var assembly = Assembly.Load("Modules.Shared");
        var expectedTypedIds = new Dictionary<string, Type>
        {
            ["GameId"] = typeof(Guid),
            ["MatchId"] = typeof(Guid),
            ["SponsorId"] = typeof(int),
            ["SponsorPlacementId"] = typeof(int),
            ["TeamId"] = typeof(Guid),
            ["TeamInviteId"] = typeof(Guid),
            ["TournamentRegistrationId"] = typeof(Guid),
            ["TournamentRosterMemberId"] = typeof(Guid),
            ["UserId"] = typeof(Guid)
        };

        foreach (var (typeName, valueType) in expectedTypedIds)
        {
            var type = assembly.GetType($"Mercurius.Modules.Shared.{typeName}", throwOnError: false, ignoreCase: false);

            Assert.NotNull(type);
            Assert.True(type!.IsValueType, $"{typeName} must be a value type.");
            Assert.Equal(valueType, type.GetProperty("Value")?.PropertyType);
        }
    }

    [Fact]
    public void IdentityUserService_DoesNotPublishIntegrationEventsDirectly()
    {
        var userServiceDependencies = GetDeclaredDependencyTypes(typeof(UserService));
        var publishingDecoratorDependencies = GetDeclaredDependencyTypes(typeof(UserIntegrationEventPublishingService));

        Assert.DoesNotContain(typeof(IModuleEventPublisher), userServiceDependencies);
        Assert.Contains(typeof(IModuleEventPublisher), publishingDecoratorDependencies);
    }

    [Fact]
    public void TeamsImplementationTypes_RemainNonPublicDuringPhase7FollowUp()
    {
        var assembly = Assembly.Load("Mercurius.Modules.Teams");
        var nonPublicTypeNames = new[]
        {
            "Mercurius.Modules.Teams.Services.TeamService",
            "Mercurius.Modules.Teams.Services.TeamEventPublishingDecorator",
            "Mercurius.Modules.Teams.Services.ITeamEndpointService",
            "Mercurius.Modules.Teams.Services.TeamEndpointService",
            "Mercurius.Modules.Teams.Services.TeamLogoStorage",
            "Mercurius.Modules.Teams.Services.ITeamLogoStorage",
            "Mercurius.Modules.Teams.Services.RealtimeTeamEventPublisher",
            "Mercurius.Modules.Teams.Services.NullTeamEventPublisher",
            "Mercurius.Modules.Teams.Services.EfTeamRealtimeAuthorizer",
            "Mercurius.Modules.Teams.Domain.TeamInvite",
            "Mercurius.Modules.Teams.Infrastructure.ITeamsDbContext",
            "Mercurius.Modules.Teams.Infrastructure.TeamsDbContextAdapter`1",
            "Mercurius.Modules.Teams.Infrastructure.TeamsModelBuilderExtensions"
        };

        foreach (var typeName in nonPublicTypeNames)
        {
            var type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);

            Assert.NotNull(type);
            Assert.False(type!.IsPublic, $"{typeName} must remain non-public.");
        }
    }

    [Fact]
    public void CompetitionEndpointTypes_AreInternalAndApiHostNoLongerOwnsThem()
    {
        var competitionAssembly = Assembly.Load("Mercurius.Modules.Competition");
        var apiAssembly = Assembly.Load("Mercurius.LAN.API");
        var endpointTypeNames = new[]
        {
            "Mercurius.Modules.Competition.Endpoints.GameEndpoints",
            "Mercurius.Modules.Competition.Endpoints.MatchEndpoints",
            "Mercurius.Modules.Competition.Endpoints.TournamentRegistrationEndpoints"
        };

        foreach (var typeName in endpointTypeNames)
        {
            var endpointType = competitionAssembly.GetType(typeName, throwOnError: false, ignoreCase: false);

            Assert.NotNull(endpointType);
            Assert.False(endpointType!.IsPublic, $"{typeName} must remain an internal module implementation detail.");
        }

        Assert.Null(apiAssembly.GetType("Mercurius.LAN.API.Endpoints.GameEndpoints", throwOnError: false));
        Assert.Null(apiAssembly.GetType("Mercurius.LAN.API.Endpoints.MatchEndpoints", throwOnError: false));
        Assert.Null(apiAssembly.GetType("Mercurius.LAN.API.Endpoints.TournamentRegistrationEndpoints", throwOnError: false));
    }

    [Fact]
    public void CompetitionApplicationInterfaces_RequireTrailingCancellationToken()
    {
        var interfaces = new[]
        {
            typeof(IGameService),
            typeof(IMatchService),
            typeof(ITournamentRegistrationService)
        };

        foreach (var interfaceType in interfaces)
        {
            var asyncMethods = interfaceType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType));

            foreach (var method in asyncMethods)
            {
                var cancellationToken = method.GetParameters().Last();

                Assert.Equal(typeof(CancellationToken), cancellationToken.ParameterType);
                Assert.True(
                    cancellationToken.HasDefaultValue,
                    $"{interfaceType.Name}.{method.Name} should default its cancellation token.");
            }
        }
    }

    private static HashSet<string> GetProjectReferences(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var project = XDocument.Load(projectPath);

        return project
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include!)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static Type[] GetDeclaredDependencyTypes(Type type)
    {
        const BindingFlags declaredMembers =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        return type
            .GetConstructors(declaredMembers)
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Concat(type.GetFields(declaredMembers).Select(field => field.FieldType))
            .ToArray();
    }

    private static IEnumerable<Type> GetPublicApiTypes(Type exportedType)
    {
        yield return exportedType;

        if (exportedType.BaseType is not null)
        {
            yield return exportedType.BaseType;
        }

        foreach (var interfaceType in exportedType.GetInterfaces())
        {
            yield return interfaceType;
        }

        const BindingFlags publicDeclaredMembers =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var constructor in exportedType.GetConstructors(publicDeclaredMembers))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }

        foreach (var method in exportedType.GetMethods(publicDeclaredMembers))
        {
            yield return method.ReturnType;

            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }

        foreach (var property in exportedType.GetProperties(publicDeclaredMembers))
        {
            yield return property.PropertyType;
        }

        foreach (var field in exportedType.GetFields(publicDeclaredMembers))
        {
            yield return field.FieldType;
        }

        foreach (var eventInfo in exportedType.GetEvents(publicDeclaredMembers))
        {
            if (eventInfo.EventHandlerType is not null)
            {
                yield return eventInfo.EventHandlerType;
            }
        }
    }

    private static bool IsForbiddenContractType(Type type)
    {
        if (type.HasElementType)
        {
            return IsForbiddenContractType(type.GetElementType()!);
        }

        if (type == typeof(IQueryable)
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
            || typeof(IQueryable).IsAssignableFrom(type))
        {
            return true;
        }

        if (type.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true)
        {
            return true;
        }

        return type.IsGenericType && type.GetGenericArguments().Any(IsForbiddenContractType);
    }

    private static string FindRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "LAN.API.sln")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root containing LAN.API.sln.");
    }
}
