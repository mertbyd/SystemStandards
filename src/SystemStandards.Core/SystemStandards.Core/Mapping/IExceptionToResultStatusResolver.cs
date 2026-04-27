namespace SystemStandards.Mapping;

using SystemStandards.Results;

/// <summary>
/// Exception → ResultStatus dönüşümü için DI-injectable resolver.
/// Hardcoded switch YASAK — mapping options'tan okunur.
/// ABP paketi bu interface'i override eder (ABP exception'ları için).
/// </summary>
public interface IExceptionToResultStatusResolver
{
    /// <summary>
    /// Exception'ı karşılık gelen ResultStatus'a çevirir.
    /// Type hiyerarşisine göre en yakın eşlemeyi döner.
    /// </summary>
    ResultStatus Resolve(Exception exception);
}
