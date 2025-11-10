using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MercuriusAPI.Extensions;

public class SecurityTrimming : IDocumentFilter
{
    private readonly IServiceProvider _provider;

    public SecurityTrimming(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var http = _provider.GetRequiredService<IHttpContextAccessor>();
        var auth = _provider.GetRequiredService<IAuthorizationService>();

        foreach (var description in context.ApiDescriptions)
        {
            var allowAnonAttributes = description.CustomAttributes().OfType<AllowAnonymousAttribute>();
            if (allowAnonAttributes.Any())
                continue; // user is allowed to access this method

            var authAttributes = description.CustomAttributes().OfType<AuthorizeAttribute>();
            bool notShown = IsForbiddenDueAnonymous(http, authAttributes) ||
                            IsForbiddenDuePolicy(http, auth, authAttributes);

            if (!notShown)
                continue; // user passed all permissions checks

            var route = "/" + description?.RelativePath?.TrimEnd('/');
            var path = swaggerDoc.Paths[route];

            // remove method or entire path (if there are no more methods in this path)
            OperationType operation;
            Enum.TryParse(description?.HttpMethod, true, out operation);
            path.Operations.Remove(operation);
            if (path.Operations.Count == 0)
            {
                swaggerDoc.Paths.Remove(route);
            }
        }
    }

    private static bool IsForbiddenDuePolicy(
        IHttpContextAccessor http,
        IAuthorizationService auth,
        IEnumerable<AuthorizeAttribute> attributes)
    {
        var policies = attributes
            .Where(p => !string.IsNullOrEmpty(p.Policy))
            .Select(a => a.Policy)
            .Distinct();

        var result = Task.WhenAll(policies.Select(p => auth.AuthorizeAsync(http?.HttpContext?.User, p))).Result;
        return result.Any(r => !r.Succeeded);
    }

    private static bool IsForbiddenDueAnonymous(
        IHttpContextAccessor http,
        IEnumerable<AuthorizeAttribute> attributes)
    {
        return attributes.Any() && (!http.HttpContext?.User?.Identity?.IsAuthenticated ?? false);
    }
}
