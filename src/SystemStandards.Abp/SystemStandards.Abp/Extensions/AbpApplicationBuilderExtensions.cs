namespace SystemStandards.Abp.Extensions;

using Microsoft.AspNetCore.Builder;
using SystemStandards.Abp.Identity;

/// <summary>
/// ABP middleware ve servislerini pipeline'a ekleyen extension method'lar.
/// </summary>
public static class AbpApplicationBuilderExtensions
{
    /// <summary>
    /// ABP-aware SystemStandards middleware'larını pipeline'a ekler.
    /// app.UseAuthentication() SONRASINA eklenmeli — ICurrentUser dolmuş olsun.
    /// Kullanım:
    /// <code>
    /// app.UseAuthentication();
    /// app.UseSystemStandardsAbp();
    /// app.UseAuthorization();
    /// </code>
    /// </summary>
    public static IApplicationBuilder UseSystemStandardsAbp(this IApplicationBuilder app)
    {
        // Authentication sonrası çalışır — ICurrentUser dolmuş
        app.UseMiddleware<AbpOperationContextEnricher>();
        return app;
    }
}
