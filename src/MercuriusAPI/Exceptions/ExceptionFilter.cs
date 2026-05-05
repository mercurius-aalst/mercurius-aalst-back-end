using Auth.Module.Exceptions;
using Mercurius.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using AuthNotFoundException = Auth.Module.Exceptions.NotFoundException;
using AuthValidationException = Auth.Module.Exceptions.ValidationException;
using SharedNotFoundException = Mercurius.Shared.Exceptions.NotFoundException;
using SharedValidationException = Mercurius.Shared.Exceptions.ValidationException;

namespace Mercurius.LAN.API.Exceptions;

public class ExceptionFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is AuthNotFoundException or SharedNotFoundException)
        {
            context.Result = new ObjectResult(context.Exception.Message)
            {
                StatusCode = (int)HttpStatusCode.NotFound
            };
            context.ExceptionHandled = true;
        }
        else if (context.Exception is AuthValidationException or SharedValidationException)
        {
            context.Result = new ObjectResult(context.Exception.Message)
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            };
            context.ExceptionHandled = true;
        }
        else if (context.Exception is InvalidCredentialsException invalidCredentialsException)
        {
            context.Result = new ObjectResult(invalidCredentialsException.Message)
            {
                StatusCode = (int)HttpStatusCode.Unauthorized
            };
            context.ExceptionHandled = true;
        }
        else if (context.Exception is LockoutException lockoutException)
        {
            context.Result = new ObjectResult(lockoutException.Message)
            {
                StatusCode = 423 // Locked
            };
            context.ExceptionHandled = true;
        }
        else if (context.Exception is UnauthorizedAccessException unauthorizedAccessException)
        {
            context.Result = new ObjectResult(unauthorizedAccessException.Message)
            {
                StatusCode = (int)HttpStatusCode.Unauthorized
            };
            context.ExceptionHandled = true;
        }
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
    }
}
