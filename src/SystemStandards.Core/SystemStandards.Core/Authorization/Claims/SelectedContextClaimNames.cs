namespace SystemStandards.Authorization.Contracts.Claims;

/// <summary>
/// Doğrulanmış access token'dan okunan seçili bağlam claim adları.
/// Bu adlar authorization server'ın claim sözleşmesiyle birebir aynıdır;
/// serbest string yerine her zaman bu sabitler kullanılır.
/// </summary>
public static class SelectedContextClaimNames
{
    /// <summary>Kullanıcı actor'ünün kimliğini taşıyan claim.</summary>
    public const string Subject = "sub";

    /// <summary>Machine/MCP actor'ünün kimliğini taşıyan claim.</summary>
    public const string ClientId = "client_id";

    /// <summary>Token'ın kapsamını taşıyan claim (boşlukla ayrılmış olabilir).</summary>
    public const string Scope = "scope";

    /// <summary>Actor türünün kararlı lookup kodunu taşıyan claim.</summary>
    public const string ActorKind = "actor_kind";

    /// <summary>Seçili kiracının kimliğini taşıyan claim.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>Seçili organizasyon biriminin kimliğini taşıyan claim.</summary>
    public const string OrganizationUnitId = "organization_unit_id";

    /// <summary>Seçili application scope'un kimliğini taşıyan claim.</summary>
    public const string ApplicationScopeId = "application_scope_id";
}
