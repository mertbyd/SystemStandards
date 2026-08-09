using SystemStandards.Authorization.Contracts.Endpoints;

namespace SystemStandards.Abp.Authorization.Options;

/// <summary>
/// Authorization server istemcisinin ayarları. <c>IOptions&lt;T&gt;</c> ile appsettings'ten okunur;
/// kod içinde sabitlenmiş adres, yol veya süre bulunmaz.
/// Eksik veya geçersiz ayar uygulamayı ayağa kaldırmaz (fail-fast).
/// </summary>
public sealed class AuthenticatorClientOptions
{
    /// <summary>Ayarların appsettings içindeki bölüm adı.</summary>
    public const string SectionName = "SystemStandards:Authenticator";

    /// <summary>
    /// Authorization server'ın kök adresi. Zorunludur; mutlak http/https URI olmalıdır.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>Karar ucunun base URL'e göreli yolu.</summary>
    public string AuthorizePath { get; set; } = AuthenticatorEndpointPaths.AuthorizeSelectedContext;

    /// <summary>Tek bir uzak deneme için üst süre sınırı.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Token'da bulunması beklenen scope. Boş bırakılırsa scope kontrolü istemci tarafında yapılmaz;
    /// yetki kararı yine authorization server'a aittir.
    /// </summary>
    public string? RequiredScope { get; set; }

    /// <summary>Geçici hatalarda yapılacak yeniden deneme sayısı.</summary>
    public int RetryAttempts { get; set; } = 2;

    /// <summary>Yeniden denemeler arasındaki temel bekleme süresi.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);
}
