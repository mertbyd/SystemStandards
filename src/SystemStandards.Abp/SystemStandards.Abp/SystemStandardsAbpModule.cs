using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SystemStandards.Abp.ExceptionHandling;
using SystemStandards.Abp.Localization;
using SystemStandards.Localization;
using SystemStandards.Mapping;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Validation;
using SystemStandards.Extensions;

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
    /// <summary>
    /// ABP-aware servis kayıtlarını yapar.
    /// DefaultExceptionToResultStatusResolver'ı ABP version'la override eder.
    /// </summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // Bütün core modülleri dahil ediyoruz, böylece uygulamada ayrı ayrı çağırmaya gerek kalmaz
        services.AddSystemStandardsCore();
        services.AddSystemStandardsAspNetCore();
        services.AddSystemStandardsValidation();

        // Concrete default resolver'ı DI'a ekliyoruz ki AbpExceptionToResultStatusResolver inject edebilsin
        services.AddSingleton<DefaultExceptionToResultStatusResolver>();

        // ABP exception'larını type referansı ile map'leyen resolver (string match değil)
        services.AddSingleton<AbpExceptionToResultStatusResolver>(sp => 
            new AbpExceptionToResultStatusResolver(
                sp.GetRequiredService<DefaultExceptionToResultStatusResolver>()));
        
        // IExceptionToResultStatusResolver istendiğinde ABP resolver verilecek (O da Default'u sarmalar)
        services.AddSingleton<IExceptionToResultStatusResolver>(sp =>
            sp.GetRequiredService<AbpExceptionToResultStatusResolver>());

        // B2 fix: Hardcoded dictionary yerine IStringLocalizer kullanan provider.
        services.AddScoped<IValidationMessageProvider, AbpValidationMessageProvider>();

        // ABP default converter validasyon detaylarini uretir; SystemStandards converter eksik ust seviye code alanini tamamlar.
        services.TryAddTransient<DefaultExceptionToErrorInfoConverter>();
        services.Replace(ServiceDescriptor.Transient<IExceptionToErrorInfoConverter, ResultBasedExceptionToErrorInfoConverter>());

        // ABP'nin exception boundary'si korunur; bu paket yalnizca converter'i SystemStandards kodlariyla zenginlestirir.
    }
}
