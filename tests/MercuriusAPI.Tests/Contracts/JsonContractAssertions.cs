using System.Text.Json;
using Xunit.Sdk;

namespace Mercurius.LAN.API.Tests.Contracts;

internal static class JsonContractAssertions
{
    public static JsonElement SerializeToElement(object value)
    {
        return JsonSerializer.SerializeToElement(value);
    }

    public static void AssertHasProperty(JsonElement json, string propertyName)
    {
        if (!ContainsProperty(json, propertyName))
            throw new XunitException($"Expected JSON property '{propertyName}' was not found.");
    }

    public static void AssertDoesNotHaveProperty(JsonElement json, string propertyName)
    {
        if (ContainsProperty(json, propertyName))
            throw new XunitException($"JSON property '{propertyName}' should not be present.");
    }

    private static bool ContainsProperty(JsonElement json, string propertyName)
    {
        if (json.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var property in json.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
