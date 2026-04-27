namespace SystemStandards.Abp.Localization;

using Microsoft.Extensions.Localization;
using SystemStandards.Localization;

/// <summary>
/// ABP'nin IStringLocalizer'ını kullanan IValidationMessageProvider implementasyonu.
/// B2 fix: Hardcoded dictionary yerine localization resource'u kullanır.
/// </summary>
public class AbpValidationMessageProvider : IValidationMessageProvider
{
    private readonly IStringLocalizer _localizer;

    /// <summary>
    /// Constructor — IStringLocalizer inject edilir (ABP DI üzerinden gelir).
    /// </summary>
    public AbpValidationMessageProvider(IStringLocalizerFactory localizerFactory)
    {
        // SystemStandards resource'u kullanılır
        _localizer = localizerFactory.Create("SystemStandards", typeof(AbpValidationMessageProvider).Assembly.GetName().Name!);
    }

    /// <summary>
    /// Error code'a uygun localized mesaj döner.
    /// IStringLocalizer üzerinden ABP localization sistemi kullanılır.
    /// </summary>
    public string GetMessage(string errorCode, string? language = "tr")
    {
        try
        {
            var localizedString = _localizer[errorCode];
            return localizedString.ResourceNotFound ? $"[{errorCode}]" : localizedString.Value;
        }
        catch
        {
            return $"[{errorCode}]";
        }
    }

    /// <summary>
    /// Tek parametre ile format edilmiş mesaj döner.
    /// </summary>
    public string GetFormattedMessage(string errorCode, object? parameter = null, string? language = "tr")
    {
        var message = GetMessage(errorCode, language);
        if (parameter is null) return message;

        try { return string.Format(message, parameter); }
        catch { return message; }
    }

    /// <summary>
    /// Birden fazla parametre ile format edilmiş mesaj döner.
    /// </summary>
    public string GetFormattedMessage(string errorCode, object?[] parameters, string? language = "tr")
    {
        var message = GetMessage(errorCode, language);
        if (parameters is null || parameters.Length == 0) return message;

        try { return string.Format(message, parameters); }
        catch { return message; }
    }
}
