namespace SystemStandards.Results;

/// <summary>
/// Tüm Result türleri tarafından implement edilecek contract.
/// Her operasyon sonucu bu interface'i sağlamalıdır.
/// Hardcoding yok — tüm properties dinamiktir.
/// </summary>
public interface IResult
{
    /// <summary>
    /// İşlem başarılı mı? (Status Ok, Created, NoContent ise true)
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Operasyon durumu (Ok, Invalid, NotFound, vb.)
    /// </summary>
    ResultStatus Status { get; }

    /// <summary>
    /// Hata mesajları listesi. Başarılı işlemde boş olur.
    /// Örn: ["Database connection failed", "Timeout occurred"]
    /// </summary>
    IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Doğrulama hataları (validation-specific). Invalid status'ta doldurulur.
    /// </summary>
    IReadOnlyList<ValidationError> ValidationErrors { get; }

    /// <summary>
    /// Başarılı işlemdeki mesaj. Örn: "Ürün başarıyla oluşturuldu"
    /// </summary>
    string? SuccessMessage { get; }

    /// <summary>
    /// İstek özgünlüğü için korelasyon ID. Logging ve debugging'de kullanılır.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Created status'unda yeni kaynağın URL'i (Location header'ında döndürülür)
    /// Örn: "/api/products/123abc"
    /// </summary>
    string? Location { get; }

    /// <summary>
    /// Döndürülen veri (eğer varsa). Generic Result&lt;T&gt;'de T tipi.
    /// </summary>
    object? GetValue();
}

/// <summary>
/// Tek bir doğrulama hatasını temsil eden yapı.
/// Field + ErrorCode + ErrorMessage = tam validation context
/// </summary>
public record ValidationError(
    /// <summary>Hatanın bulunduğu DTO alanı. Örn: "Name", "Email"</summary>
    string Identifier,

    /// <summary>Hata mesajı (localized). Örn: "Ad boş olamaz"</summary>
    string ErrorMessage,

    /// <summary>Hata kodu (programmatic). Örn: "VALIDATION_REQUIRED", "EMAIL_INVALID"</summary>
    string ErrorCode = "VALIDATION_ERROR",

    /// <summary>Hatanın önem düzeyi (Error, Warning, Info)</summary>
    ValidationSeverity Severity = ValidationSeverity.Error
);

/// <summary>
/// Doğrulama hatasının önem düzeyi
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Ölümcül hata, işlem yapılamaz</summary>
    Error = 0,

    /// <summary>Uyarı, işlem yapılabilir ama sorun var</summary>
    Warning = 1,

    /// <summary>Bilgi amaçlı</summary>
    Info = 2
}
