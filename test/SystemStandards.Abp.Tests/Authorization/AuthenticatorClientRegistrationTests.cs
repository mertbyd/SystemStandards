using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SystemStandards.Abp.Authorization.Authorization;
using SystemStandards.Abp.Authorization.Context;
using SystemStandards.Abp.Authorization.Extensions;
using SystemStandards.Abp.Authorization.Options;
using SystemStandards.Abp.Authorization.Tests.Fakes;
using Volo.Abp.Authorization;
using Xunit;

namespace SystemStandards.Abp.Authorization.Tests;

/// <summary>
/// Kabul 12 — yapılandırma eksikse uygulama ayağa kalkmaz.
/// Ayrıca tek çağrılık kurulumun ne kaydettiğini ve ikinci çağrının etkisiz olduğunu doğrular.
/// </summary>
public class AuthenticatorClientRegistrationTests
{
    // Kabul 12: BaseUrl yapilandirilmamissa host baslatilamaz.
    [Fact]
    public async Task A_missing_base_url_stops_the_application_from_starting()
    {
        using var host = AuthorizationTestHost.Build(
            new FakeAuthenticatorEndpoint(),
            new RecordingLoggerProvider(),
            settings => settings[$"{AuthenticatorClientOptions.SectionName}:BaseUrl"] = null);

        var start = async () => await host.StartAsync();

        await start.Should().ThrowAsync<OptionsValidationException>();
    }

    // Gecersiz bir adres de ayni sekilde ayaga kalkmayi engeller.
    [Fact]
    public async Task A_relative_base_url_stops_the_application_from_starting()
    {
        using var host = AuthorizationTestHost.Build(
            new FakeAuthenticatorEndpoint(),
            new RecordingLoggerProvider(),
            settings => settings[$"{AuthenticatorClientOptions.SectionName}:BaseUrl"] = "/authenticator");

        var start = async () => await host.StartAsync();

        await start.Should().ThrowAsync<OptionsValidationException>();
    }

    // Tek cagri sarmalayicinin butun parcalarini kaydeder.
    [Fact]
    public void The_single_call_registers_every_moving_part()
    {
        var services = BuildServices();

        services.AddAuthenticatorAuthorization(BuildConfiguration());

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISelectedContextAccessor>().Should().BeOfType<HttpContextSelectedContextAccessor>();
        provider.GetRequiredService<ISelectedContextAuthorizer>().Should().BeOfType<HttpSelectedContextAuthorizer>();
        provider.GetServices<IAuthorizationHandler>().Should().ContainItemsAssignableTo<SelectedContextAuthorizationHandler>();
        provider.GetRequiredService<IHttpClientFactory>().Should().NotBeNull();
        provider.GetRequiredService<IAuthorizationPolicyProvider>()
            .Should().BeOfType<SelectedContextAuthorizationPolicyProvider>();
    }

    /* ABP'nin policy saglayicisi yerine gecilmez, sarmalanir: tanimadigimiz her policy adi
     * icteki saglayiciya iner. Bu test o sozlesmeyi ABP'nin kendi arayuzu uzerinden kilitler. */
    [Fact]
    public async Task An_existing_abp_policy_provider_is_wrapped_not_replaced()
    {
        var inner = new SpyAbpPolicyProvider();
        var services = BuildServices();
        services.AddSingleton<IAuthorizationPolicyProvider>(inner);
        services.AddSingleton<IAbpAuthorizationPolicyProvider>(inner);

        services.AddAuthenticatorAuthorization(BuildConfiguration());

        using var provider = services.BuildServiceProvider();

        var decorated = provider.GetRequiredService<IAbpAuthorizationPolicyProvider>();
        decorated.Should().BeOfType<SelectedContextAuthorizationPolicyProvider>();

        await decorated.GetPolicyAsync("Abp.Permission.Name");
        (await decorated.GetPoliciesNamesAsync()).Should().Equal(SpyAbpPolicyProvider.KnownPolicyName);

        inner.RequestedPolicyNames.Should().Equal("Abp.Permission.Name");
    }

    // ABP'nin saglayicisinin yerine gecen, cagrilari kaydeden test ikizi.
    private sealed class SpyAbpPolicyProvider : IAbpAuthorizationPolicyProvider
    {
        public const string KnownPolicyName = "Abp.Permission.Name";

        private readonly List<string> _requestedPolicyNames = [];

        public IReadOnlyList<string> RequestedPolicyNames => _requestedPolicyNames;

        public bool AllowsCachingPolicies => false;

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => Task.FromResult<AuthorizationPolicy?>(null);

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            _requestedPolicyNames.Add(policyName);

            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        public Task<List<string>> GetPoliciesNamesAsync() => Task.FromResult(new List<string> {KnownPolicyName});
    }

    // Modul ve elle cagri ayni container'da bulusursa ikinci kayit yapilmaz.
    [Fact]
    public void A_second_call_registers_nothing_more()
    {
        var services = BuildServices();
        var configuration = BuildConfiguration();

        services.AddAuthenticatorAuthorization(configuration);
        var afterFirstCall = services.Count;

        services.AddAuthenticatorAuthorization(configuration);

        services.Count.Should().Be(afterFirstCall);
    }

    // Testlerde kullanilan gecerli yapilandirma.
    private static IConfiguration BuildConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{AuthenticatorClientOptions.SectionName}:BaseUrl"] = "https://authenticator.test"
            })
            .Build();

    // Sarmalayicinin bagli oldugu en kucuk servis kumesi.
    private static ServiceCollection BuildServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        return services;
    }
}
