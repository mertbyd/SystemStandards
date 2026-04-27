namespace SystemStandards.Middleware;

using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Her HTTP request'te OperationContext oluşturan ve ayarlayan middleware.
/// Correlation ID (W3C Trace Context öncelikli), user info, operation name set eder.
/// Pipeline'ın EN BAŞINDA olmalı — context dolmadan diğer middleware'ler çalışmasın.
/// </summary>
public class OperationContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperationContextMiddleware> _logger;

    /// <summary>
    /// Constructor — RequestDelegate ve Logger inject edilir.
    /// </summary>
    public OperationContextMiddleware(
        RequestDelegate next,
        ILogger<OperationContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Middleware invoke — her request burada başlar.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IOperationContextAccessor contextAccessor)
    {
        // Step 1: OperationContext oluştur
        var operationCtx = new OperationContext
        {
            // W3C Trace Context önce (OpenTelemetry uyumlu), fallback X-Correlation-Id
            CorrelationId = ExtractCorrelationId(context),

            // B3 fix: ClaimTypes.NameIdentifier kullan ("sub" değil)
            // ABP paketinde AbpOperationContextEnricher authentication'dan sonra ICurrentUser ile override eder
            UserId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier),

            // Operation name — HTTP method + path
            OperationName = $"{context.Request.Method} {context.Request.Path}",

            // Tenant ID — header veya claim'den al (optional)
            TenantId = ExtractTenantId(context)
        };

        // Step 2: Context'i AsyncLocal'e yaz
        contextAccessor.SetContext(operationCtx);

        // Step 3: Response header'ına Correlation ID ekle (client'a dönsün)
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Correlation-Id"))
            {
                context.Response.Headers.Append("X-Correlation-Id", operationCtx.CorrelationId);
            }
            return Task.CompletedTask;
        });

        _logger.LogInformation(
            "Operation started: {OperationName} | CorrelationId: {CorrelationId} | UserId: {UserId}",
            operationCtx.OperationName,
            operationCtx.CorrelationId,
            operationCtx.UserId ?? "anonymous");

        try
        {
            await _next(context);

            _logger.LogInformation(
                "Operation completed: {OperationName} | StatusCode: {StatusCode} | ElapsedMs: {ElapsedMs}ms",
                operationCtx.OperationName,
                context.Response.StatusCode,
                (DateTime.UtcNow - operationCtx.StartedAt).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Operation failed: {OperationName} | CorrelationId: {CorrelationId}",
                operationCtx.OperationName,
                operationCtx.CorrelationId);

            throw;
        }
    }

    /// <summary>
    /// W3C Trace Context → X-Correlation-Id header → yeni GUID öncelik sırası.
    /// Activity.Current.TraceId W3C standardı — OpenTelemetry uyumlu.
    /// </summary>
    private static string ExtractCorrelationId(HttpContext context)
    {
        // 1. W3C Activity.Current.TraceId (OpenTelemetry ile otomatik dolar)
        var activityTraceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(activityTraceId) && activityTraceId != "00000000000000000000000000000000")
            return activityTraceId;

        // 2. X-Correlation-Id request header
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId)
            && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // 3. Yeni GUID oluştur
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// X-Tenant-Id header'ından veya claim'den Tenant ID alır (multi-tenancy).
    /// </summary>
    private static Guid? ExtractTenantId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId)
            && Guid.TryParse(tenantId.ToString(), out var headerGuid))
        {
            return headerGuid;
        }

        var tenantClaim = context.User?.FindFirstValue("tenant_id");
        if (tenantClaim is not null && Guid.TryParse(tenantClaim, out var claimGuid))
            return claimGuid;

        return null;
    }
}
