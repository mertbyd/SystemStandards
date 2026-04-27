namespace SystemStandards.Localization;

/// <summary>
/// Validation hata mesajlarını dinamik olarak sağlayan provider.
/// FluentValidation rules'ı için localized mesajlar döner.
/// Hardcoding yok — tüm mesajlar localization resource'dan gelir.
/// </summary>
public interface IValidationMessageProvider
{
    /// <summary>
    /// Validation error code'una uygun mesaj döner
    /// Örn: "REQUIRED" → "Bu alan zorunludur"
    /// </summary>
    string GetMessage(string errorCode, string? language = "tr");

    /// <summary>
    /// Mesaja parametre bind et
    /// Örn: "FIELD_MAX_LENGTH" + {0: "100"} → "Alan maksimum 100 karakter olmalıdır"
    /// </summary>
    string GetFormattedMessage(string errorCode, object? parameter = null, string? language = "tr");

    /// <summary>
    /// Birden fazla parametre ile mesaj bind et
    /// Örn: "RANGE_ERROR" + {0: "min", 1: "max"} → format etme
    /// </summary>
    string GetFormattedMessage(string errorCode, object?[] parameters, string? language = "tr");
}
