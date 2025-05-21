using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;

namespace WMS.Backend.WebApi.Endpoints
{
    public class DtoValidator
    {
        public static ProblemDetails? Validate<TDto>(TDto dto, HttpContext httpContext)
        {
            if (dto is not null)
            {
                var validationContext = new ValidationContext(dto);
                var validationResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
                {
                    return new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest),
                        Detail = "Validation Error",
                        Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                        Extensions = new Dictionary<string, object?> { { "validationResults", validationResults } }
                    };
                }
            }

            return null;
        }
    }
}
