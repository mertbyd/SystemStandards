using System.Text.Json.Serialization;

namespace SystemStandards.Authorization.Contracts.Contracts;

/// <summary>
/// Karar ucunun ürettiği yanıt gövdesi.
/// Ret bir hata değildir: HTTP 200 ve <see cref="IsAllowed"/> = false ile döner,
/// gerekçe <see cref="FailureCode"/> içinde taşınır.
/// </summary>
public sealed class AuthorizationDecisionContract
{
    /// <summary>Actor bu bağlamda sorulan operasyonu yapabilir mi.</summary>
    [JsonPropertyName("isAllowed")]
    public bool IsAllowed { get; init; }

    /// <summary>Ret gerekçesinin kararlı kodu; izin verildiğinde null kalır.</summary>
    [JsonPropertyName("failureCode")]
    public string? FailureCode { get; init; }

    /// <summary>Kararın üretildiği bağlam sürümü; ret durumunda sıfırdır.</summary>
    [JsonPropertyName("contextVersion")]
    public long ContextVersion { get; init; }

    /// <summary>Kararın alındığı erişim tavanının kararlı lookup kodu.</summary>
    [JsonPropertyName("applicationScopeLevelCode")]
    public string? ApplicationScopeLevelCode { get; init; }

    /// <summary>Karar privileged dalda alındı mı; audit zorunluluğunu belirler.</summary>
    [JsonPropertyName("requiresPrivilegedAudit")]
    public bool RequiresPrivilegedAudit { get; init; }
}
