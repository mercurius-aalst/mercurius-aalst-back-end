using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mercurius.Platform.Swagger;

internal sealed class SecurityTrimming(IServiceProvider provider) : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var http = provider.GetRequiredService<IHttpContextAccessor>();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        foreach (var description in context.ApiDescriptions)
        {
            var allowAnonymousAttributes = description.CustomAttributes().OfType<AllowAnonymousAttribute>();
            if (allowAnonymousAttributes.Any())
                continue;

            var authorizeAttributes = description.CustomAttributes().OfType<AuthorizeAttribute>();
            var shouldHide = IsAnonymousForbidden(http, authorizeAttributes) ||
                             IsPolicyForbidden(http, authorization, authorizeAttributes);

            if (!shouldHide)
                continue;

            var route = "/" + description.RelativePath?.TrimEnd('/');
            var path = swaggerDoc.Paths[route];
            Enum.TryParse(description.HttpMethod, true, out OperationType operation);
            path.Operations.Remove(operation);
            if (path.Operations.Count == 0)
                swaggerDoc.Paths.Remove(route);
        }
    }

    private static bool IsPolicyForbidden(
        IHttpContextAccessor http,
        IAuthorizationService authorization,
        IEnumerable<AuthorizeAttribute> attributes)
    {
        var policies = attributes
            .Where(attribute => !string.IsNullOrEmpty(attribute.Policy))
            .Select(attribute => attribute.Policy!)
            .Distinct();

        var results = Task.WhenAll(policies.Select(policy =>
            authorization.AuthorizeAsync(http.HttpContext!.User, policy))).Result;

        return results.Any(result => !result.Succeeded);
    }

    private static bool IsAnonymousForbidden(
        IHttpContextAccessor http,
        IEnumerable<AuthorizeAttribute> attributes)
    {
        return attributes.Any() && (!http.HttpContext?.User?.Identity?.IsAuthenticated ?? false);
    }
}
