using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace MercuriusAPI.Exceptions
{
    public class ExceptionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {         
            if (context.Exception is NotFoundException entityNotFoundException)
            {
                context.Result = new ObjectResult(entityNotFoundException.Message)
                {
                    StatusCode = (int)HttpStatusCode.NotFound
                };
                context.ExceptionHandled = true;
            }
            else if(context.Exception is ValidationException validationException)
            {
                context.Result = new ObjectResult(validationException.Message)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
                context.ExceptionHandled = true;
            }
            else if(context.Exception is InvalidCredentialsException invalidCredentialsException)
            {
                context.Result = new ObjectResult(invalidCredentialsException.Message)
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized
                };
                context.ExceptionHandled = true;
            }
            else if(context.Exception is LockoutException lockoutException)
            {
                context.Result = new ObjectResult(lockoutException.Message)
                {
                    StatusCode = 423 // Locked
                };
                context.ExceptionHandled = true;
            }           
            else if(context.Exception is UnauthorizedAccessException unauthorizedAccessException)
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
}
