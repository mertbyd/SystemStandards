namespace SystemStandards.Abp.Authorization.Extensions;

/// <summary>
/// Kayıt işleminin bir kez yapıldığını gösteren işaretçi.
/// ABP modülü ve elle çağrılan <c>AddAuthenticatorAuthorization</c> aynı container'da buluşabilir;
/// bu işaretçi olmadan named client'a resilience handler'ı iki kez eklenir.
/// </summary>
internal sealed class AuthenticatorAuthorizationMarker;
