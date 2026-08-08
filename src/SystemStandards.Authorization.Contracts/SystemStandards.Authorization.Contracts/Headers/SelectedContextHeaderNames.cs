namespace SystemStandards.Authorization.Contracts.Headers;

/// <summary>
/// Privileged (host-global) akışta taşınan istek header adları.
/// İstemci bu header'ları üretmez; çağırandan geldiği gibi authorization server'a iletir.
/// </summary>
public static class SelectedContextHeaderNames
{
    /// <summary>Host-global işlemin neden yapıldığını taşıyan zorunlu gerekçe.</summary>
    public const string PrivilegedReason = "X-Authenticator-Privileged-Reason";

    /// <summary>Host-global işlemin audit izini kuran zorunlu correlation kimliği.</summary>
    public const string CorrelationId = "X-Authenticator-Correlation-Id";
}
