using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SystemStandards.Abp.Authorization.Options;
using SystemStandards.Authorization.Contracts.Claims;
using SystemStandards.Authorization.Contracts.FailureCodes;
using SystemStandards.Authorization.Contracts.Headers;

namespace SystemStandards.Abp.Authorization.Context;

/// <summary>
/// Seçili bağlamı çalışan HTTP isteğinin doğrulanmış principal'ından okuyan adapter.
/// Çeviri tam olarak bir yerde yapılır; her claim kendi alanına okunur ve eksik ya da bozuk
/// claim'de gerekçe kodu taşıyan başarısız sonuç döner.
/// </summary>
public sealed class HttpContextSelectedContextAccessor : ISelectedContextAccessor
{
    // Authorization header'inda beklenen sema; token bu onekten sonra baslar.
    private const string BearerPrefix = "Bearer ";

    // Scope claim'i tek claim icinde bosluklarla ayrilmis da gelebilir, ayri claim'ler halinde de.
    private static readonly char[] ScopeSeparators = [' '];

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<AuthenticatorClientOptions> _options;

    /// <summary>Bağımlılıkları alır.</summary>
    public HttpContextSelectedContextAccessor(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AuthenticatorClientOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    /// <inheritdoc />
    public SelectedContextResolution Resolve()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.Rejected);
        }

        var principal = httpContext.User;
        if (principal?.Identity is not { IsAuthenticated: true })
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.Rejected);
        }

        var accessToken = ReadBearerToken(httpContext.Request);
        if (accessToken is null)
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.Rejected);
        }

        var actorKindCode = ReadClaim(principal, SelectedContextClaimNames.ActorKind);
        if (actorKindCode is null)
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.MissingActor);
        }

        /* Actor kimligi kullanici icin sub, machine client icin client_id claim'inde durur.
         * Actor turu kodlarinin anlami sunucuya aittir; istemci yalnizca "hicbir kimlik claim'i yok"
         * halini reddeder, tur ile kimligin eslesip eslesmedigine authorization server karar verir. */
        var actorIdentifier = ReadClaim(principal, SelectedContextClaimNames.Subject)
                              ?? ReadClaim(principal, SelectedContextClaimNames.ClientId);
        if (actorIdentifier is null)
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.UnsupportedActor);
        }

        if (!HasRequiredScope(principal))
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.InvalidScope);
        }

        /* Asagidaki gerekce kodlari authorization server'in ayni claim icin urettigi kodlarla birebir
         * ayni tutulur: ayni kosul iki tarafta iki farkli kod uretirse gerekce sozlesmesi ayrisir. */
        if (!TryReadRequiredIdentifier(principal, SelectedContextClaimNames.ApplicationScopeId, out var applicationScopeId))
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.MissingApplicationScope);
        }

        if (!TryReadRequiredIdentifier(principal, SelectedContextClaimNames.TenantId, out var tenantId))
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.MalformedTenant);
        }

        if (!TryReadRequiredIdentifier(principal, SelectedContextClaimNames.OrganizationUnitId, out var organizationUnitId))
        {
            return SelectedContextResolution.Failed(SelectedContextFailureCodes.AuthContext.MalformedOrganizationUnit);
        }

        return SelectedContextResolution.Resolved(new SelectedContext
        {
            ActorKindCode = actorKindCode,
            ActorIdentifier = actorIdentifier,
            TenantId = tenantId,
            OrganizationUnitId = organizationUnitId,
            ApplicationScopeId = applicationScopeId,
            AccessToken = accessToken,
            PrivilegedReason = ReadHeader(httpContext.Request, SelectedContextHeaderNames.PrivilegedReason),
            CorrelationId = ReadHeader(httpContext.Request, SelectedContextHeaderNames.CorrelationId)
        });
    }

    // Authorization header'indaki bearer token'i cikarir; sema uymuyorsa null doner.
    private static string? ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[BearerPrefix.Length..].Trim();

        return token.Length == 0 ? null : token;
    }

    // Header yoksa veya bos ise null doner.
    private static string? ReadHeader(HttpRequest request, string headerName)
    {
        var value = request.Headers[headerName].ToString();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    // Claim yoksa veya bos ise null doner; bos string bir deger sayilmaz.
    private static string? ReadClaim(ClaimsPrincipal principal, string claimName)
    {
        var value = principal.FindFirst(claimName)?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    // Zorunlu bir bağlam claim'ini bos olmayan Guid olarak cozer.
    private static bool TryReadRequiredIdentifier(ClaimsPrincipal principal, string claimName, out Guid identifier)
    {
        identifier = Guid.Empty;

        var value = ReadClaim(principal, claimName);
        if (value is null)
        {
            return false;
        }

        if (!Guid.TryParse(value, out var parsed) || parsed == Guid.Empty)
        {
            return false;
        }

        identifier = parsed;

        return true;
    }

    // Yapilandirilmis bir scope varsa token'in o scope'u tasiyip tasimadigini dogrular.
    private bool HasRequiredScope(ClaimsPrincipal principal)
    {
        var requiredScope = _options.Value.RequiredScope;
        if (string.IsNullOrWhiteSpace(requiredScope))
        {
            return true;
        }

        foreach (var claim in principal.FindAll(SelectedContextClaimNames.Scope))
        {
            var granted = claim.Value.Split(ScopeSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (granted.Contains(requiredScope, StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
