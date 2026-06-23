using System.Reflection;
using System.Xml.Linq;

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
    public void ModuleImplementation_ReferencesOnlyOwnContractsAndModulesShared(string moduleName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var moduleDirectory = Path.Combine(repositoryRoot, "src", "Modules", moduleName);
        var implementationProject = Path.Combine(
            moduleDirectory,
            $"Mercurius.Modules.{moduleName}",
            $"Mercurius.Modules.{moduleName}.csproj");
        var references = GetProjectReferences(implementationProject);
        var expectedReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(
                moduleDirectory,
                $"Mercurius.Modules.{moduleName}.Contracts",
                $"Mercurius.Modules.{moduleName}.Contracts.csproj"),
            Path.Combine(repositoryRoot, "src", "Modules.Shared", "Modules.Shared.csproj")
        };

        Assert.True(
            expectedReferences.SetEquals(references),
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
