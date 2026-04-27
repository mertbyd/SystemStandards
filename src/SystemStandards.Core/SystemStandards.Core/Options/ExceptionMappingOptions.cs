namespace SystemStandards.Options;

using SystemStandards.Results;

/// <summary>
/// Exception type → ResultStatus eşlemelerini tutan options.
/// DI'dan alınır, default'lar kod içinde tanımlanır — hardcoded switch YASAK.
/// ABP paketi kendi exception'larını ekler.
/// </summary>
public class ExceptionMappingOptions
{
    /// <summary>
    /// Exception type → ResultStatus eşleme tablosu.
    /// Base type inheritance desteklenir (DefaultExceptionToResultStatusResolver).
    /// </summary>
    public Dictionary<Type, ResultStatus> Mappings { get; set; } = new()
    {
        { typeof(ArgumentException), ResultStatus.Invalid },
        { typeof(ArgumentNullException), ResultStatus.Invalid },
        { typeof(UnauthorizedAccessException), ResultStatus.Unauthorized },
        { typeof(TimeoutException), ResultStatus.Unavailable },
        { typeof(NotImplementedException), ResultStatus.Error },
        { typeof(InvalidOperationException), ResultStatus.Conflict },
        { typeof(KeyNotFoundException), ResultStatus.NotFound }
    };

    /// <summary>
    /// Mapping bulunamazsa kullanılacak varsayılan status.
    /// </summary>
    public ResultStatus DefaultStatus { get; set; } = ResultStatus.Error;
}
