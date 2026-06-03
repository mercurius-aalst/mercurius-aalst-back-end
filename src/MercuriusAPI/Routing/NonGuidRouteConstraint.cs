using System.Globalization;
using Microsoft.AspNetCore.Routing;

namespace Mercurius.LAN.API.Routing;

public sealed class NonGuidRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var routeValue) || routeValue is null)
            return false;

        var routeText = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
        return !Guid.TryParse(routeText, out _);
    }
}
