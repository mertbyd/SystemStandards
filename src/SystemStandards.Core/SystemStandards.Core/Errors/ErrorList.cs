namespace SystemStandards.Errors;

/// <summary>
/// Birden fazla hata mesajını, CorrelationId ile birlikte tutan yardımcı sınıf.
/// Hata gruplarını tek bir nesne olarak taşımak için kullanılır.
/// Örn: new ErrorList(["Hata 1", "Hata 2"], correlationId)
/// </summary>
public class ErrorList : List<string>
{
    /// <summary>
    /// Bu hata listesiyle ilişkili istek korelasyon ID'si.
    /// Logging ve tracing'de kullanılır.
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Hata listesi oluşturur.
    /// </summary>
    /// <param name="errors">Hata mesajları</param>
    /// <param name="correlationId">İstek korelasyon ID'si</param>
    public ErrorList(IEnumerable<string> errors, string correlationId = "")
        : base(errors)
    {
        CorrelationId = correlationId;
    }
}
