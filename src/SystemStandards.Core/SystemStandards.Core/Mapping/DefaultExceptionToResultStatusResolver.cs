namespace SystemStandards.Mapping;

using Microsoft.Extensions.Options;
using SystemStandards.Options;
using SystemStandards.Results;

/// <summary>
/// ExceptionMappingOptions'tan okunan exception → ResultStatus varsayılan resolver.
/// Type hiyerarşisini dolaşır — base type eşlemesi de desteklenir.
/// ABP exception'ları için Abp paketi bu sınıfı extends eder.
/// </summary>
public class DefaultExceptionToResultStatusResolver : IExceptionToResultStatusResolver
{
    private readonly ExceptionMappingOptions _options;

    /// <summary>
    /// Constructor — ExceptionMappingOptions DI ile inject edilir.
    /// </summary>
    public DefaultExceptionToResultStatusResolver(IOptions<ExceptionMappingOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Exception type'ını ResultStatus'a çevirir.
    /// Doğrudan eşleme yoksa base type'ları dener, sonunda DefaultStatus döner.
    /// </summary>
    public ResultStatus Resolve(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var type = exception.GetType();
        while (type is not null)
        {
            if (_options.Mappings.TryGetValue(type, out var status))
                return status;

            type = type.BaseType;
        }

        return _options.DefaultStatus;
    }
}
