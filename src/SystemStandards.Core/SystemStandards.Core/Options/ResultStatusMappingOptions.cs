namespace SystemStandards.Options;

/// <summary>
/// ResultStatus'ları HTTP codes, ABP error codes, ve mesajlara eşleyen dinamik mapping.
/// Hiçbir hardcoding yok — tüm değerler appsettings.json'dan yüklenir.
/// Örn: services.Configure<ResultStatusMappingOptions>(configuration.GetSection(...))
/// </summary>
public class ResultStatusMappingOptions
{
    /// <summary>
    /// ResultStatus enum'unu HTTP status code'una eşleyen mapping tablosu.
    /// appsettings.json'dan yüklenir.
    /// </summary>
    public List<StatusToHttpMapping> StatusToHttpCodeMap { get; set; } = new();

    /// <summary>
    /// ResultStatus'ını ABP error code'una (InventoryTracking:Product.NotFound) eşler.
    /// appsettings.json'dan yüklenir.
    /// </summary>
    public List<StatusToAbpErrorCodeMapping> StatusToAbpErrorCodeMap { get; set; } = new();

    /// <summary>
    /// Default HTTP status code (mapping bulunamadıysa kullanılır)
    /// Örn: 500 (Internal Server Error)
    /// </summary>
    public int DefaultHttpStatusCode { get; set; } = 500;

    /// <summary>
    /// Default ABP error code (mapping bulunamadıysa kullanılır)
    /// Örn: "SystemStandards:General.UnknownError"
    /// </summary>
    public string DefaultAbpErrorCode { get; set; } = "SystemStandards:General.UnknownError";
}

/// <summary>
/// Tek bir ResultStatus'ını HTTP status code'una eşler.
/// appsettings.json örneği:
/// { "resultStatus": "NotFound", "httpStatusCode": 404 }
/// </summary>
public class StatusToHttpMapping
{
    /// <summary>Ardalis ResultStatus enum değeri</summary>
    public string ResultStatus { get; set; } = string.Empty;

    /// <summary>Karşılık gelen HTTP status code (int)</summary>
    public int HttpStatusCode { get; set; }
}

/// <summary>
/// Tek bir ResultStatus'ını ABP error code'una eşler.
/// appsettings.json örneği:
/// { "resultStatus": "Invalid", "abpErrorCode": "InventoryTracking:Validation.Failed" }
/// </summary>
public class StatusToAbpErrorCodeMapping
{
    /// <summary>Ardalis ResultStatus enum değeri</summary>
    public string ResultStatus { get; set; } = string.Empty;

    /// <summary>ABP error code (namespace:Feature.ErrorType)</summary>
    public string AbpErrorCode { get; set; } = string.Empty;
}
