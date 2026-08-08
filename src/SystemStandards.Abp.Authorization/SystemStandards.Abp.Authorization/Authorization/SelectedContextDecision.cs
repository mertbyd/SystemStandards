using SystemStandards.Authorization.Contracts.Contracts;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Bir operasyon için verilen authorization kararı.
/// Ret bir hata değildir; gerekçesini <see cref="FailureCode"/> içinde taşır.
/// </summary>
public sealed class SelectedContextDecision
{
    private SelectedContextDecision(
        bool isAllowed,
        string? failureCode,
        long contextVersion,
        string? applicationScopeLevelCode,
        bool requiresPrivilegedAudit)
    {
        IsAllowed = isAllowed;
        FailureCode = failureCode;
        ContextVersion = contextVersion;
        ApplicationScopeLevelCode = applicationScopeLevelCode;
        RequiresPrivilegedAudit = requiresPrivilegedAudit;
    }

    /// <summary>Operasyona izin verildi mi.</summary>
    public bool IsAllowed { get; }

    /// <summary>Ret gerekçesinin kararlı kodu; izin verildiğinde null kalır.</summary>
    public string? FailureCode { get; }

    /// <summary>Kararın üretildiği bağlam sürümü; ret durumunda sıfırdır.</summary>
    public long ContextVersion { get; }

    /// <summary>Kararın alındığı erişim tavanının kararlı lookup kodu.</summary>
    public string? ApplicationScopeLevelCode { get; }

    /// <summary>Karar privileged dalda alındı mı.</summary>
    public bool RequiresPrivilegedAudit { get; }

    /// <summary>Verilen gerekçe koduyla reddedilmiş karar üretir.</summary>
    public static SelectedContextDecision Denied(string failureCode)
        => new(
            isAllowed: false,
            failureCode,
            contextVersion: 0,
            applicationScopeLevelCode: null,
            requiresPrivilegedAudit: false);

    /// <summary>Uzak karar gövdesini süreç içi karara çevirir.</summary>
    public static SelectedContextDecision FromContract(AuthorizationDecisionContract contract)
        => new(
            contract.IsAllowed,
            contract.IsAllowed ? null : contract.FailureCode,
            contract.ContextVersion,
            contract.ApplicationScopeLevelCode,
            contract.RequiresPrivilegedAudit);
}
