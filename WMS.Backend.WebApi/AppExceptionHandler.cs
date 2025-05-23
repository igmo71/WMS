using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using WMS.Backend.Common;

namespace WMS.Backend.WebApi
{
    internal class AppExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var status = exception switch
            {
                BadHttpRequestException or ArgumentException => StatusCodes.Status400BadRequest,
                NotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError
            };

            httpContext.Response.StatusCode = status;

            var problemDetails = new ProblemDetails
            {
                Status = status,
                Type = ReasonPhrases.GetReasonPhrase(status),
                Title = exception.GetType().Name,
                Detail = exception.Message,
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            };

            //await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            //return true;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                Exception = exception,
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
        }
    }
}
