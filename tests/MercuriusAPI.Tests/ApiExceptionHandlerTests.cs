using System.Text;
using Auth.Module.Exceptions;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Middleware;
using Mercurius.Shared.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Mercurius.LAN.API.Tests;

public class ApiExceptionHandlerTests
{
    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public async Task TryHandleAsync_MapsKnownExceptions(Exception exception, int expectedStatusCode)
    {
        var handler = new ApiExceptionHandler();
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        var responseBody = await new StreamReader(httpContext.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.Contains(exception.Message, responseBody);
    }

    public static IEnumerable<object[]> KnownExceptions()
    {
        yield return [new Auth.Module.Exceptions.ValidationException("Validation failed."), StatusCodes.Status400BadRequest];
        yield return [new Auth.Module.Exceptions.NotFoundException("Missing."), StatusCodes.Status404NotFound];
        yield return [new InvalidCredentialsException("Nope."), StatusCodes.Status401Unauthorized];
        yield return [new LockoutException(), StatusCodes.Status423Locked];
        yield return [new UnauthorizedAccessException("Denied."), StatusCodes.Status401Unauthorized];
    }
}
