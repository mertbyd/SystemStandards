namespace SystemStandards.Abp.ExceptionHandling;

using SystemStandards.Mapping;
using SystemStandards.Results;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;

/// <summary>
/// ABP exception type'larını type referansı ile ResultStatus'a eşleyen resolver.
/// String match YASAK — type referansı kullanılır.
/// IExceptionToResultStatusResolver'ı extend eder (base için DefaultExceptionToResultStatusResolver'a delege).
/// </summary>
public class AbpExceptionToResultStatusResolver : IExceptionToResultStatusResolver
{
    private readonly IExceptionToResultStatusResolver _defaultResolver;

    /// <summary>
    /// Constructor — base resolver inject edilir.
    /// </summary>
    public AbpExceptionToResultStatusResolver(IExceptionToResultStatusResolver defaultResolver)
    {
        _defaultResolver = defaultResolver;
    }

    /// <summary>
    /// ABP exception'larını ResultStatus'a map'ler.
    /// Önce ABP-specific exception'lar denenir, sonra default resolver'a delege edilir.
    /// </summary>
    public ResultStatus Resolve(Exception exception) => exception switch
    {
        EntityNotFoundException => ResultStatus.NotFound,
        AbpAuthorizationException => ResultStatus.Forbidden,
        AbpValidationException => ResultStatus.Invalid,
        _ => _defaultResolver.Resolve(exception)
    };
}
