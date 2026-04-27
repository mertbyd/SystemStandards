namespace SystemStandards.Abp.Identity;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SystemStandards.Middleware;
using Volo.Abp.Users;

/// <summary>
/// ABP'nin ICurrentUser'ından gerçek UserId, TenantId çekip OperationContext'e ekleyen enricher.
/// app.UseAuthentication() SONRASINA eklenir — User.Claims authentication'dan sonra dolar.
/// B3 fix'in ABP tarafı: ICurrentUser kullanır, claim string'i değil.
/// </summary>
public class AbpOperationContextEnricher
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructor — RequestDelegate inject edilir.
    /// </summary>
    public AbpOperationContextEnricher(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// ICurrentUser'dan gerçek UserId ve TenantId alır, OperationContext'i günceller.
    /// Authentication sonrası çalışır.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        IOperationContextAccessor contextAccessor,
        ICurrentUser currentUser)
    {
        var operationCtx = contextAccessor.GetContext();
        if (operationCtx is not null && currentUser.IsAuthenticated)
        {
            // ICurrentUser — ABP standardı, ABP claim'lerini doğru okur
            operationCtx.UserId = currentUser.Id?.ToString();
            operationCtx.TenantId = currentUser.TenantId;
        }

        await _next(context);
    }
}
