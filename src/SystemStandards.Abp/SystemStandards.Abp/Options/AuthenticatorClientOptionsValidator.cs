using Microsoft.Extensions.Options;

namespace SystemStandards.Abp.Authorization.Options;

/// <summary>
/// <see cref="AuthenticatorClientOptions"/> için fail-fast doğrulayıcı.
/// <c>ValidateOnStart</c> ile birlikte çalışır: eksik veya geçersiz ayarda uygulama ayağa kalkmaz.
/// Yanlış yapılandırılmış bir yetki istemcisiyle çalışmaya devam etmek, açık kapı bırakmakla aynıdır.
/// </summary>
public sealed class AuthenticatorClientOptionsValidator : IValidateOptions<AuthenticatorClientOptions>
{
    /// <summary>Ayarları doğrular; her ihlal ayrı bir mesaj olarak toplanır.</summary>
    public ValidateOptionsResult Validate(string? name, AuthenticatorClientOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            failures.Add($"{AuthenticatorClientOptions.SectionName}:{nameof(AuthenticatorClientOptions.BaseUrl)} zorunludur.");
        }
        else if (!IsAbsoluteHttpUri(options.BaseUrl))
        {
            failures.Add($"{AuthenticatorClientOptions.SectionName}:{nameof(AuthenticatorClientOptions.BaseUrl)} mutlak bir http/https adresi olmalidir.");
        }

        if (string.IsNullOrWhiteSpace(options.AuthorizePath))
        {
            failures.Add($"{AuthenticatorClientOptions.SectionName}:{nameof(AuthenticatorClientOptions.AuthorizePath)} bos olamaz.");
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            failures.Add($"{AuthenticatorClientOptions.SectionName}:{nameof(AuthenticatorClientOptions.Timeout)} sifirdan buyuk olmalidir.");
        }

        if (options.RetryAttempts < 1)
        {
            failures.Add($"{AuthenticatorClientOptions.SectionName}:{nameof(AuthenticatorClientOptions.RetryAttempts)} en az 1 olmalidir.");
        }

        if (options.RetryDelay < TimeSpan.Zero)
        {
            failures.Add($"{AuthenticatorClientOptions.SectionName}:{nameof(AuthenticatorClientOptions.RetryDelay)} negatif olamaz.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    // Adresin mutlak ve http/https olup olmadigini dogrular.
    private static bool IsAbsoluteHttpUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
