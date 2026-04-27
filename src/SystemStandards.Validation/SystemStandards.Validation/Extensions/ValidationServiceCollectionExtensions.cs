namespace SystemStandards.Extensions;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SystemStandards.Validation;

/// <summary>
/// IServiceCollection'a SystemStandards Validation servislerini register eden extensions.
/// Not: IValidationMessageProvider implementasyonu kullanıcı tarafından sağlanmalıdır.
/// ABP projelerinde SystemStandardsAbpModule bu implementasyonu otomatik olarak sağlar (AbpValidationMessageProvider).
/// </summary>
public static class ValidationServiceCollectionExtensions
{
    /// <summary>
    /// SystemStandards Validation servislerini DI container'a ekler.
    /// IValidationMessageProvider implementasyonu sağlanmazsa çalışma zamanında hata alınır.
    /// Kullanım: builder.Services.AddSystemStandardsValidation();
    /// </summary>
    public static IServiceCollection AddSystemStandardsValidation(
        this IServiceCollection services,
        Action<IServiceCollection>? configureFluentValidation = null)
    {
        // FluentValidation — ValidatorFactory ile auto-discovery (bu assembly'deki validator'lar)
        services.AddValidatorsFromAssembly(typeof(FluentValidationExtensions).Assembly);

        // Optional: custom FluentValidation configuration
        configureFluentValidation?.Invoke(services);

        return services;
    }
}
