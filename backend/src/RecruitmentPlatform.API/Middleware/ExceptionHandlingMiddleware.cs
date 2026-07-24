using System.Text.Json;
using RecruitmentPlatform.Application.Common.Exceptions;

namespace RecruitmentPlatform.API.Middleware;

/// <summary>
/// Converts unhandled exceptions into consistent RFC7807-style JSON problem responses and maps
/// known application exceptions to the appropriate HTTP status codes.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, errors) = exception switch
        {
            ValidationException v => (StatusCodes.Status400BadRequest, v.Message, v.Errors),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, null),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message, null),
            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message, null),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, exception.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", (IReadOnlyDictionary<string, string[]>?)null)
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("{Status} on {Method} {Path}: {Message}", status, context.Request.Method, context.Request.Path, exception.Message);
        }

        var problem = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.io/{status}",
            ["title"] = title,
            ["status"] = status,
            ["traceId"] = context.TraceIdentifier
        };

        if (errors is not null)
        {
            problem["errors"] = errors;
        }

        // Only surface stack traces in development.
        if (status == StatusCodes.Status500InternalServerError && _environment.IsDevelopment())
        {
            problem["detail"] = exception.ToString();
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
