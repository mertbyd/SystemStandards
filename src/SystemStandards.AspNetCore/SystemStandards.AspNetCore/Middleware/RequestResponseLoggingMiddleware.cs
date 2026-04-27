namespace SystemStandards.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// HTTP request ve response'ları detaylı ve standardize loglayan middleware.
/// İşlem süresi (elapsed time) ve Correlation ID ile her isteği trace eder.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    /// <summary>
    /// Constructor — RequestDelegate ve Logger inject edilir.
    /// </summary>
    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Middleware invoke — request/response döngüsünü loglar.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IOperationContextAccessor contextAccessor)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var operationCtx = contextAccessor.GetContext();
        var correlationId = operationCtx?.CorrelationId ?? "N/A";

        _logger.LogInformation(
            "HTTP Request Started | CorrelationId: {CorrelationId} | Method: {Method} | Path: {Path}{QueryString}",
            correlationId,
            request.Method,
            request.Path,
            request.QueryString);

        try
        {
            await _next(context);
            
            stopwatch.Stop();
            var response = context.Response;

            _logger.LogInformation(
                "HTTP Response Finished | CorrelationId: {CorrelationId} | Method: {Method} | Path: {Path} | Status: {StatusCode} | Elapsed: {ElapsedMs}ms",
                correlationId,
                request.Method,
                request.Path,
                response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "HTTP Response Failed | CorrelationId: {CorrelationId} | Method: {Method} | Path: {Path} | Elapsed: {ElapsedMs}ms",
                correlationId,
                request.Method,
                request.Path,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
