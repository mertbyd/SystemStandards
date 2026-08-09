namespace SystemStandards.Abp.ExceptionHandling;

using SystemStandards.Mapping;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.Http;

/// <summary>
/// ABP'nin ürettiği RemoteServiceErrorInfo çıktısını SystemStandards ResultStatus kodlarıyla zenginleştirir.
/// </summary>
public class ResultBasedExceptionToErrorInfoConverter : IExceptionToErrorInfoConverter
{
    private readonly DefaultExceptionToErrorInfoConverter _defaultConverter;
    private readonly IExceptionToResultStatusResolver _statusResolver;

    /// <summary>Default ABP converter korunur; SystemStandards yalnız eksik üst seviye kodu tamamlar.</summary>
    public ResultBasedExceptionToErrorInfoConverter(
        DefaultExceptionToErrorInfoConverter defaultConverter,
        IExceptionToResultStatusResolver statusResolver)
    {
        _defaultConverter = defaultConverter;
        _statusResolver = statusResolver;
    }

    /// <summary>Exception nesnesini ABP'nin standart hata sözleşmesine çevirir.</summary>
    public RemoteServiceErrorInfo Convert(Exception exception, bool includeSensitiveDetails)
    {
        var errorInfo = _defaultConverter.Convert(exception, includeSensitiveDetails);
        EnsureResultStatusCode(exception, errorInfo);
        return errorInfo;
    }

    /// <summary>Exception nesnesini ek ABP seçenekleriyle standart hata sözleşmesine çevirir.</summary>
    public RemoteServiceErrorInfo Convert(Exception exception, Action<AbpExceptionHandlingOptions>? options = null)
    {
        var errorInfo = _defaultConverter.Convert(exception, options);
        EnsureResultStatusCode(exception, errorInfo);
        return errorInfo;
    }

    // ABP kod üretmediyse SystemStandards durum kodunu üst seviye hata kodu yapar.
    private void EnsureResultStatusCode(Exception exception, RemoteServiceErrorInfo errorInfo)
    {
        if (!string.IsNullOrWhiteSpace(errorInfo.Code))
        {
            return;
        }

        errorInfo.Code = _statusResolver
            .Resolve(exception)
            .ToString()
            .ToUpperInvariant();
    }
}
