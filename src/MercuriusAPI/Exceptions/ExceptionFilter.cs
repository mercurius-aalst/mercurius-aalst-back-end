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
            if(context.Exception is ValidationException validationException)
            {
                context.Result = new ObjectResult(validationException.Message)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };

                context.ExceptionHandled = true;
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {

        }
    }
}
