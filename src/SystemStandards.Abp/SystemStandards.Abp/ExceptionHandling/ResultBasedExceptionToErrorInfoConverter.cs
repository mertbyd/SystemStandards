namespace SystemStandards.Abp.ExceptionHandling;

using SystemStandards.Results;
using Volo.Abp.Http;
using Volo.Abp.Http.Modeling;

/// <summary>
/// SystemStandards Result hata formatını ABP'nin RemoteServiceErrorInfo contract'ına çeviren converter.
/// ABP Angular/Blazor/MAUI frontend'leri bu yapıyı parse ettiğinden uyumluluk kritik.
/// </summary>
public class ResultBasedExceptionToErrorInfoConverter
{
    /// <summary>
    /// Result Status'unu RemoteServiceErrorInfo'ya çevirir.
    /// </summary>
    public RemoteServiceErrorInfo Convert(ResultStatus status, string? message, string? details = null)
    {
        return new RemoteServiceErrorInfo
        {
            Code = status.ToString().ToUpperInvariant(),
            Message = message ?? GetDefaultMessage(status),
            Details = details
        };
    }

    private static string GetDefaultMessage(ResultStatus status) => status switch
    {
        ResultStatus.NotFound => "Kaynak bulunamadı",
        ResultStatus.Forbidden => "Erişim yasaklı",
        ResultStatus.Unauthorized => "Lütfen oturum açın",
        ResultStatus.Invalid => "Doğrulama hatası",
        ResultStatus.Conflict => "Kaynak çakışması",
        ResultStatus.Unavailable => "Servis geçici olarak kullanılamaz",
        _ => "Bir hata oluştu"
    };
}
