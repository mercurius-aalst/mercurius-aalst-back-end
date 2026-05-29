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

    public static void AssertDoesNotContainProperty(JsonElement json, string propertyName)
    {
        if (ContainsPropertyDeep(json, propertyName))
            throw new XunitException($"JSON property '{propertyName}' should not be present anywhere in the payload.");
    }

    public static void AssertContainsProperty(JsonElement json, string propertyName)
    {
        if (!ContainsPropertyDeep(json, propertyName))
            throw new XunitException($"Expected JSON property '{propertyName}' was not found anywhere in the payload.");
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

    private static bool ContainsPropertyDeep(JsonElement json, string propertyName)
    {
        switch (json.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in json.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (ContainsPropertyDeep(property.Value, propertyName))
                        return true;
                }
                return false;
            case JsonValueKind.Array:
                return json.EnumerateArray().Any(item => ContainsPropertyDeep(item, propertyName));
            default:
                return false;
        }
    }
}
