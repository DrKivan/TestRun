using System.Net;
using System.Text.Json;

namespace EvaluaT.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentOutOfRangeException => (HttpStatusCode.BadRequest, "Invalid request"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid request"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Authentication failed"),
            InvalidOperationException => (HttpStatusCode.Conflict, "Operation could not be completed"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource was not found"),
            NotSupportedException => (HttpStatusCode.BadRequest, "Unsupported option"),
            _ => (HttpStatusCode.InternalServerError, "Unexpected server error")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception.");
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var payload = new
        {
            type = "https://httpstatuses.com/" + (int)statusCode,
            title,
            status = (int)statusCode,
            detail = exception.Message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
