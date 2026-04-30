using Mercurius.LAN.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Mercurius.LAN.API.Middleware;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            InvalidCredentialsException => StatusCodes.Status401Unauthorized,
            LockoutException => StatusCodes.Status423Locked,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => (int?)null
        };

        if (!statusCode.HasValue)
            return false;

        httpContext.Response.StatusCode = statusCode.Value;
        await httpContext.Response.WriteAsJsonAsync(exception.Message, cancellationToken);
        return true;
    }
}
