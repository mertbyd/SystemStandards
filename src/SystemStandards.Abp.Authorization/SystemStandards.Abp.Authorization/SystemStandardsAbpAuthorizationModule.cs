using Microsoft.Extensions.DependencyInjection;
using SystemStandards.Abp.Authorization.Extensions;
using Volo.Abp.Authorization;
using Volo.Abp.Modularity;

namespace SystemStandards.Abp.Authorization;

/// <summary>
/// Tenant + organizasyon birimi + application scope yetkilendirmesini bir ABP projesine
/// tek <c>DependsOn</c> ile kazandıran modül.
/// </summary>
/// <remarks>
/// Kullanım:
/// <code>
/// [DependsOn(typeof(SystemStandardsAbpAuthorizationModule))]
/// public class MyModule : AbpModule { }
/// </code>
/// Modül kayıtları kendisi yapar; <c>AddAuthenticatorAuthorization(configuration)</c> çağrısı
/// açıkça yazılırsa ikinci kayıt yapılmaz.
/// </remarks>
[DependsOn(
    typeof(SystemStandardsAbpModule),
    typeof(AbpAuthorizationModule))]
public class SystemStandardsAbpAuthorizationModule : AbpModule
{
    /// <summary>Sarmalayıcının bütün kayıtlarını uygulama konfigürasyonundan kurar.</summary>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAuthenticatorAuthorization(context.Services.GetConfiguration());
    }
}
