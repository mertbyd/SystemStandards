using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SystemStandards.Abp.ExceptionHandling;
using SystemStandards.Abp.Localization;
using SystemStandards.Extensions;
using SystemStandards.Localization;
using SystemStandards.Mapping;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;

namespace SystemStandards.Abp;

/// <summary>
/// ABP Framework entegrasyonu için SystemStandards modülü.
/// DependsOn ile projeye ekleyin:
/// [DependsOn(typeof(SystemStandardsAbpModule))]
/// </summary>
[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpIdentityDomainSharedModule),
    typeof(AbpValidationModule)
)]
public class SystemStandardsAbpModule : AbpModule
{
    /// <summary>ABP-aware SystemStandards servis kayıtlarını yapar.</summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.AddSystemStandardsCore();
        services.AddSystemStandardsAspNetCore();
        services.AddSystemStandardsValidation();

        services.AddSingleton<DefaultExceptionToResultStatusResolver>();
        services.AddSingleton<AbpExceptionToResultStatusResolver>(serviceProvider =>
            new AbpExceptionToResultStatusResolver(
                serviceProvider.GetRequiredService<DefaultExceptionToResultStatusResolver>()));
        services.AddSingleton<IExceptionToResultStatusResolver>(serviceProvider =>
            serviceProvider.GetRequiredService<AbpExceptionToResultStatusResolver>());

        services.AddScoped<IValidationMessageProvider, AbpValidationMessageProvider>();

        services.TryAddTransient<DefaultExceptionToErrorInfoConverter>();
        services.Replace(
            ServiceDescriptor.Transient<IExceptionToErrorInfoConverter, ResultBasedExceptionToErrorInfoConverter>());

        // Authorization istemcisi isteğe bağlıdır; AddAuthenticatorAuthorization çağırmayan tüketicilere ayar zorunluluğu getirilmez.
    }
}
