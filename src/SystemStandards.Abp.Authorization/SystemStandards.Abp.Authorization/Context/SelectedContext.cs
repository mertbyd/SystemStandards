namespace SystemStandards.Abp.Authorization.Context;

/// <summary>
/// Doğrulanmış token'dan typed olarak okunmuş seçili bağlam.
/// Bu tip yalnız süreç içinde taşınır; loglanmaz ve serialize edilmez —
/// <see cref="AccessToken"/> çağıranın canlı bearer token'ıdır.
/// </summary>
public sealed class SelectedContext
{
    /// <summary>Actor türünün kararlı lookup kodu.</summary>
    public required string ActorKindCode { get; init; }

    /// <summary>Actor'ün kimliği (kullanıcı için <c>sub</c>, machine client için <c>client_id</c>).</summary>
    public required string ActorIdentifier { get; init; }

    /// <summary>Seçili kiracının kimliği.</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Seçili organizasyon biriminin kimliği.</summary>
    public required Guid OrganizationUnitId { get; init; }

    /// <summary>Seçili application scope'un kimliği.</summary>
    public required Guid ApplicationScopeId { get; init; }

    /// <summary>Çağıranın bearer token'ı; uzak karar çağrısına aynen iletilir. Asla loglanmaz.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Privileged akış gerekçesi; istek header'ında geldiyse dolu olur.</summary>
    public string? PrivilegedReason { get; init; }

    /// <summary>Privileged akış correlation kimliği; istek header'ında geldiyse dolu olur.</summary>
    public string? CorrelationId { get; init; }
}
