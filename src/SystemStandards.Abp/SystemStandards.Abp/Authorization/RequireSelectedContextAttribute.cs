using Microsoft.AspNetCore.Authorization;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Korunan action veya application service metodunu seçili bağlam kararına bağlar.
/// Kullanım: <c>[RequireSelectedContext(MyPermissions.Sources.Manage)]</c>
/// </summary>
/// <remarks>
/// Attribute operasyon adını bir policy adına çevirir; requirement'ı
/// <see cref="SelectedContextAuthorizationPolicyProvider"/> üretir.
/// <para>
/// Requirement'ı attribute'ün doğrudan taşıması (<c>IAuthorizationRequirementData</c>) bilerek
/// seçilmedi: <c>AuthorizationPolicy.CombineAsync</c> requirement verisini okumaz ve ABP'nin
/// method invocation authorization'ı tam olarak o yolu kullanır. Bu durumda attribute application
/// service metotlarında sessizce etkisiz kalırdı. Policy adı ise her iki yolda da çözülür.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequireSelectedContextAttribute : AuthorizeAttribute
{
    /// <summary>Attribute'ü izni sorulan operasyon adıyla kurar.</summary>
    /// <param name="operationName">İzni sorulan operasyonun kararlı adı (permission adı).</param>
    public RequireSelectedContextAttribute(string operationName)
        : base(SelectedContextPolicyNames.Build(operationName))
    {
        OperationName = operationName;
    }

    /// <summary>İzni sorulan operasyonun kararlı adı.</summary>
    public string OperationName { get; }
}
