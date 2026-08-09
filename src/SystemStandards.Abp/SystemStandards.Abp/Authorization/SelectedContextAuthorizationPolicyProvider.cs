using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Authorization;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Seçili bağlam policy adlarını çözen, geri kalan her adı içteki sağlayıcıya devreden decorator.
/// </summary>
/// <remarks>
/// ABP'nin (veya framework'ün) policy sağlayıcısı yerine geçmez, sarmalar: tanımadığı her policy adı
/// olduğu gibi içeriye iner, böylece ABP permission policy'leri çalışmaya devam eder.
/// <see cref="IAbpAuthorizationPolicyProvider"/> de uygulanır; ABP'nin method invocation
/// authorization'ı policy'yi bu arayüz üzerinden çözer.
/// </remarks>
public sealed class SelectedContextAuthorizationPolicyProvider : IAbpAuthorizationPolicyProvider
{
    private readonly IAuthorizationPolicyProvider _inner;

    /// <summary>Sarmalanacak sağlayıcıyı alır.</summary>
    /// <param name="inner">Tanınmayan policy adlarının devredileceği sağlayıcı.</param>
    public SelectedContextAuthorizationPolicyProvider(IAuthorizationPolicyProvider inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
    }

    /// <inheritdoc />
    public bool AllowsCachingPolicies => _inner.AllowsCachingPolicies;

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!SelectedContextPolicyNames.TryReadOperationName(policyName, out var operationName))
        {
            return _inner.GetPolicyAsync(policyName);
        }

        return Task.FromResult<AuthorizationPolicy?>(BuildPolicy(operationName));
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _inner.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _inner.GetFallbackPolicyAsync();

    /// <inheritdoc />
    public Task<List<string>> GetPoliciesNamesAsync()
    {
        /* Bu sarmalayicinin policy adlari operasyon adi kadar acik uclu oldugu icin sayilamaz;
         * listelenebilir adlar icteki saglayiciya aittir. */
        return _inner is IAbpAuthorizationPolicyProvider abpProvider
            ? abpProvider.GetPoliciesNamesAsync()
            : Task.FromResult(new List<string>());
    }

    // Operasyon adindan policy uretir: once kimlik, sonra secili baglam karari.
    private static AuthorizationPolicy BuildPolicy(string operationName)
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new SelectedContextRequirement(operationName))
            .Build();
}
