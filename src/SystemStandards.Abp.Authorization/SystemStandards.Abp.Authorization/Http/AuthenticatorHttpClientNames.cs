namespace SystemStandards.Abp.Authorization.Http;

/// <summary>
/// <c>IHttpClientFactory</c> üzerinden çözülen named client adları.
/// </summary>
public static class AuthenticatorHttpClientNames
{
    /// <summary>Authorization karar ucunu tüketen client.</summary>
    public const string Decision = "SystemStandards.Authenticator.Decision";
}
