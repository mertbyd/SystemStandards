namespace SystemStandards.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SystemStandards.Mapping;

/// <summary>
/// Yakalanmamış exception'ları RFC 7807 ProblemDetails formatında döndüren middleware.
/// GlobalExceptionMiddleware'in yerine geçer — hardcoded switch YASAK.
/// Exception → ResultStatus dönüşümü IExceptionToResultStatusResolver üzerinden yapılır.
/// </summary>
public class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    /// <summary>
    /// Constructor — RequestDelegate ve Logger inject edilir.
    /// </summary>
    public ProblemDetailsMiddleware(
        RequestDelegate next,
        ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Middleware invoke — exception'ları yakala, ProblemDetails olarak döndür.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex,
                    "Exception occurred but response already started — {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                throw;
            }

            _logger.LogError(ex,
                "İşlenmeyen exception — {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // IExceptionToResultStatusResolver DI'dan al (hardcoded switch YOK — B1 fix)
        var resolver = context.RequestServices
            .GetRequiredService<IExceptionToResultStatusResolver>();
        var map = context.RequestServices
            .GetRequiredService<ResultStatusMap>();

        var resultStatus = resolver.Resolve(exception);

        var statusCode = map.ContainsKey(resultStatus)
            ? (int)map[resultStatus].DefaultStatusCode
            : 500;

        // CorrelationId al (OperationContextMiddleware tarafından set edilmiş)
        string? correlationId = null;
        if (context.Response.Headers.TryGetValue("X-Correlation-Id", out var corrId))
            correlationId = corrId.ToString();

        // RFC 7807 ProblemDetails
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = exception.Message,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        if (correlationId is not null)
            problem.Extensions["correlationId"] = correlationId;

        problem.Extensions["exceptionType"] = exception.GetType().Name;
        problem.Extensions["resultStatus"] = resultStatus.ToString();

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problem);
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        500 => "Internal Server Error",
        503 => "Service Unavailable",
        _ => "An error occurred"
    };
}
