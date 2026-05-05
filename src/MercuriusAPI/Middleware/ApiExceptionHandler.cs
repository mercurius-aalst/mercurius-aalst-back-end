using Auth.Module.Exceptions;
using Mercurius.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using AuthNotFoundException = Auth.Module.Exceptions.NotFoundException;
using AuthValidationException = Auth.Module.Exceptions.ValidationException;
using SharedNotFoundException = Mercurius.Shared.Exceptions.NotFoundException;
using SharedValidationException = Mercurius.Shared.Exceptions.ValidationException;

namespace Mercurius.LAN.API.Middleware;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            AuthNotFoundException or SharedNotFoundException => StatusCodes.Status404NotFound,
            AuthValidationException or SharedValidationException => StatusCodes.Status400BadRequest,
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
