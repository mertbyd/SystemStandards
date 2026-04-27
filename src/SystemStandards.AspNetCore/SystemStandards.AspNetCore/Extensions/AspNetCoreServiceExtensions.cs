namespace SystemStandards.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SystemStandards.Conventions;
using SystemStandards.Mapping;
using SystemStandards.Middleware;

/// <summary>
/// IServiceCollection ve IApplicationBuilder'a SystemStandards ASP.NET Core servislerini ekleyen extensions.
/// Middleware, filter, accessor'ları register eder.
/// </summary>
public static class AspNetCoreServiceExtensions
{
    /// <summary>
    /// SystemStandards ASP.NET Core servislerini DI container'a ekler.
    /// Kullanım: builder.Services.AddSystemStandardsAspNetCore();
    /// </summary>
    public static IServiceCollection AddSystemStandardsAspNetCore(
        this IServiceCollection services)
    {
        // B11 fix: Singleton (static AsyncLocal ile uyumlu)
        services.AddSingleton<IOperationContextAccessor, OperationContextAccessor>();

        // ResultConvention — Swagger entegrasyonu için
        services.AddSingleton<ResultConvention>();

        return services;
    }

    /// <summary>
    /// SystemStandards middleware'larını pipeline'a ekler.
    /// B4 fix — Middleware sırası:
    ///   1. OperationContextMiddleware  — CorrelationId önce dolsun
    ///   2. RequestResponseLoggingMiddleware — log (CorrelationId artık DOLU)
    ///   3. ProblemDetailsMiddleware   — son güvence, RFC 7807 hata yanıtı
    /// Kullanım: app.UseSystemStandardsAspNetCore();
    /// </summary>
    public static IApplicationBuilder UseSystemStandardsAspNetCore(
        this IApplicationBuilder app)
    {
        // B4 fix: Context ÖNCE, sonra Logging, sonra Exception handler
        app.UseMiddleware<OperationContextMiddleware>();      // 1 — context önce
        app.UseMiddleware<RequestResponseLoggingMiddleware>(); // 2 — log (CorrelationId dolu)
        app.UseMiddleware<ProblemDetailsMiddleware>();         // 3 — son güvence

        return app;
    }
}
