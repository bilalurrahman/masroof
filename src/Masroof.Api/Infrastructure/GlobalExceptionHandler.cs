using FluentValidation;
using Masroof.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Masroof.Api.Infrastructure;

/// <summary>Maps application exceptions to RFC 9457 ProblemDetails responses.</summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = Map(ex);

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Unhandled exception");
        else
            logger.LogInformation("Request failed: {Title} ({Status})", title, status);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex is ValidationException ? "One or more validation errors occurred." : ex.Message,
            Type = $"https://httpstatuses.io/{status}"
        };

        switch (ex)
        {
            case ValidationException ve:
                problem.Extensions["errors"] = ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                break;
            case DuplicateTransactionException dup:
                problem.Extensions["existingTransactionId"] = dup.ExistingTransactionId;
                break;
            case UnparseableLlmOutputException { RawOutput: { Length: > 0 } raw }:
                problem.Extensions["rawOutput"] = raw.Length > 2000 ? raw[..2000] : raw;
                break;
        }

        ctx.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = ctx,
            Exception = ex,
            ProblemDetails = problem
        });
    }

    private static (int Status, string Title) Map(Exception ex) => ex switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
        NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
        DuplicateTransactionException => (StatusCodes.Status409Conflict, "Duplicate transaction"),
        UnparseableLlmOutputException => (StatusCodes.Status422UnprocessableEntity, "Could not parse message"),
        LlmUnavailableException => (StatusCodes.Status502BadGateway, "LLM unavailable"),
        FormatException => (StatusCodes.Status400BadRequest, "Invalid request"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
    };
}
