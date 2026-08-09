using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using SystemStandards.Abp.Authorization.Authorization;
using SystemStandards.Abp.Authorization.Context;
using SystemStandards.Abp.Authorization.Http;
using SystemStandards.Abp.Authorization.Options;
using Volo.Abp.Authorization;

namespace SystemStandards.Abp.Authorization.Extensions;

/// <summary>
/// Authenticator authorization sarmalayıcısını tek çağrıyla kuran extension'lar.
/// </summary>
public static class AuthenticatorAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Options, named HttpClient, bağlam okuyucu, authorizer ve authorization handler kayıtlarını yapar.
    /// Aynı container'da ikinci kez çağrılması etkisizdir.
    /// </summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">Ayarların okunacağı konfigürasyon.</param>
    public static IServiceCollection AddAuthenticatorAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(AuthenticatorAuthorizationMarker)))
        {
            return services;
        }

        services.AddSingleton<AuthenticatorAuthorizationMarker>();

        // Ayar eksik veya bozuksa uygulama ayaga kalkmaz; yanlis yapilandirilmis yetki istemcisiyle calisilmaz.
        services.AddOptions<AuthenticatorClientOptions>()
            .Bind(configuration.GetSection(AuthenticatorClientOptions.SectionName))
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AuthenticatorClientOptions>, AuthenticatorClientOptionsValidator>());

        services.AddHttpContextAccessor();

        // Handler'in calisabilmesi icin authorization cekirdegi yeter; policy ve middleware
        // kayitlari uygulamanin (ya da ABP'nin) kendi isidir.
        services.AddAuthorizationCore();

        DecoratePolicyProviders(services);

        AddDecisionClient(services);

        services.TryAddScoped<ISelectedContextAccessor, HttpContextSelectedContextAccessor>();
        services.TryAddScoped<ISelectedContextAuthorizer, HttpSelectedContextAuthorizer>();
        services.AddScoped<IAuthorizationHandler, SelectedContextAuthorizationHandler>();

        return services;
    }

    /* Policy saglayicilarini sarmalar. Iki kayit birden sarmalanir: ASP.NET Core middleware'i
     * IAuthorizationPolicyProvider, ABP'nin method invocation authorization'i ise
     * IAbpAuthorizationPolicyProvider uzerinden policy cozer. Yalniz biri sarmalanirsa
     * attribute diger yolda sessizce etkisiz kalir. */
    private static void DecoratePolicyProviders(IServiceCollection services)
    {
        DecoratePolicyProvider(services, typeof(IAuthorizationPolicyProvider));
        DecoratePolicyProvider(services, typeof(IAbpAuthorizationPolicyProvider));
    }

    // Verilen servis tipinin son kaydini decorator ile degistirir; kayit yoksa bir sey yapmaz.
    private static void DecoratePolicyProvider(IServiceCollection services, Type serviceType)
    {
        var descriptor = services.LastOrDefault(item => item.ServiceType == serviceType);
        if (descriptor is null)
        {
            return;
        }

        if (descriptor.ImplementationType == typeof(SelectedContextAuthorizationPolicyProvider))
        {
            return;
        }

        services.Remove(descriptor);
        services.Add(ServiceDescriptor.Describe(
            serviceType,
            serviceProvider => new SelectedContextAuthorizationPolicyProvider(
                ResolveInnerProvider(serviceProvider, descriptor)),
            descriptor.Lifetime));
    }

    // Sarmalanan kaydin ornegini, kaydin kendi kurulum bicimine sadik kalarak uretir.
    private static IAuthorizationPolicyProvider ResolveInnerProvider(
        IServiceProvider serviceProvider,
        ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IAuthorizationPolicyProvider instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return (IAuthorizationPolicyProvider)descriptor.ImplementationFactory(serviceProvider);
        }

        return (IAuthorizationPolicyProvider)ActivatorUtilities.CreateInstance(
            serviceProvider,
            descriptor.ImplementationType!);
    }

    // Karar ucunu tuketen named client'i kurar; dayaniklilik standart handler'a birakilir.
    private static void AddDecisionClient(IServiceCollection services)
    {
        services
            .AddHttpClient(AuthenticatorHttpClientNames.Decision, (serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AuthenticatorClientOptions>>().Value;

                client.BaseAddress = BuildBaseAddress(options.BaseUrl!);

                /* Sureleri resilience pipeline yonetir. HttpClient.Timeout acik birakilirsa
                 * toplam istek suresinden once devreye girip pipeline'i etkisiz kilar. */
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler()
            .Configure((resilienceOptions, serviceProvider) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AuthenticatorClientOptions>>().Value;

                resilienceOptions.AttemptTimeout.Timeout = options.Timeout;
                resilienceOptions.TotalRequestTimeout.Timeout = CalculateTotalTimeout(options);
                resilienceOptions.CircuitBreaker.SamplingDuration = CalculateSamplingDuration(options.Timeout);

                resilienceOptions.Retry.MaxRetryAttempts = options.RetryAttempts;
                resilienceOptions.Retry.Delay = options.RetryDelay;
                resilienceOptions.Retry.BackoffType = DelayBackoffType.Constant;
            });
    }

    // Base adresi sonu bolu ile normalize eder; aksi halde goreli yol son segmenti ezer.
    private static Uri BuildBaseAddress(string baseUrl)
        => new(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");

    // Butun denemelerin ve beklemelerin sigacagi ust sinir.
    private static TimeSpan CalculateTotalTimeout(AuthenticatorClientOptions options)
        => options.Timeout * (options.RetryAttempts + 1)
           + options.RetryDelay * (options.RetryAttempts + 2)
           + TimeSpan.FromSeconds(1);

    // Standart handler ornekleme suresinin deneme suresinin en az iki kati olmasini ister.
    private static TimeSpan CalculateSamplingDuration(TimeSpan attemptTimeout)
    {
        var minimum = TimeSpan.FromMilliseconds(500);
        var doubled = attemptTimeout * 2;

        return doubled < minimum ? minimum : doubled;
    }
}
