using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Korunan action veya application service metodunu seçili bağlam kararına bağlar.
/// Kullanım: <c>[RequireSelectedContext(MyPermissions.Sources.Manage)]</c>
/// </summary>
/// <remarks>
/// Attribute kendi requirement'larını taşır (<see cref="IAuthorizationRequirementData"/>), bu yüzden
/// her operasyon adı için ayrı policy tanımlamaya ya da policy provider'ı değiştirmeye gerek kalmaz.
/// ASP.NET Core tarafında authorization middleware, ABP tarafında authorization interceptor aynı
/// requirement'ı çalıştırır: ABP'nin sağladığı zincir değiştirilmez, üstüne yazılır.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireSelectedContextAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    /// <summary>Attribute'ü izni sorulan operasyon adıyla kurar.</summary>
    /// <param name="operationName">İzni sorulan operasyonun kararlı adı (permission adı).</param>
    public RequireSelectedContextAttribute(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        OperationName = operationName;
    }

    /// <summary>İzni sorulan operasyonun kararlı adı.</summary>
    public string OperationName { get; }

    /// <inheritdoc />
    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        /* Anonim istek karar ucuna hic gitmez: kimlik yoksa cevap 401'dir, 403 degil.
         * Bu requirement olmadan attribute default policy'yi devre disi birakir ve
         * anonim istek handler'a kadar gelir. */
        yield return new DenyAnonymousAuthorizationRequirement();

        yield return new SelectedContextRequirement(OperationName);
    }
}
