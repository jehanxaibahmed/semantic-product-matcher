using Microsoft.AspNetCore.Diagnostics;
using ProductMatcher.Application.Matching;

namespace ProductMatcher.Api;

/// <summary>Maps application validation errors to 400 problem details.</summary>
internal sealed class ValidationExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not MatchValidationException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Title = "Invalid request", Detail = exception.Message, Status = 400 },
        });
    }
}
