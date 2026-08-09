namespace SystemStandards.Authorization.Contracts.Endpoints;

/// <summary>
/// Authorization server üzerindeki kararlı endpoint yolları (base URL'e göreli).
/// Yollar konfigürasyondan geçersiz kılınabilir; buradaki değerler sözleşmenin varsayılanıdır.
/// </summary>
public static class AuthenticatorEndpointPaths
{
    /// <summary>Seçili bağlamda operasyon iznini soran karar ucu.</summary>
    public const string AuthorizeSelectedContext = "api/authenticator/contexts/authorize";
}
