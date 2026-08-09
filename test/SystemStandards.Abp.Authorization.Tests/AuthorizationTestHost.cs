using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SystemStandards.Abp.Authorization.Extensions;
using SystemStandards.Abp.Authorization.Http;
using SystemStandards.Abp.Authorization.Options;
using SystemStandards.Abp.Authorization.Tests.App;
using SystemStandards.Abp.Authorization.Tests.Fakes;

namespace SystemStandards.Abp.Authorization.Tests;

/// <summary>
/// Sarmalayıcıyı gerçek bir ASP.NET Core hattında çalıştıran test barındırıcısı.
/// Uzak karar ucu sahte bir primary handler ile karşılanır; ağa çıkılmaz ama istek
/// authorization middleware'inden ve resilience pipeline'ından gerçekten geçer.
/// </summary>
internal sealed class AuthorizationTestHost : IAsyncDisposable
{
    private readonly IHost _host;
    private HttpClient? _client;

    private AuthorizationTestHost(IHost host, FakeAuthenticatorEndpoint authenticator, RecordingLoggerProvider logs)
    {
        _host = host;
        Authenticator = authenticator;
        Logs = logs;
    }

    /// <summary>Sahte karar ucu.</summary>
    public FakeAuthenticatorEndpoint Authenticator { get; }

    /// <summary>Toplanan log satırları.</summary>
    public RecordingLoggerProvider Logs { get; }

    /// <summary>Test uygulamasına istek yapan istemci.</summary>
    public HttpClient Client => _client ??= _host.GetTestClient();

    /// <summary>Barındırıcıyı kurar ve başlatır.</summary>
    public static async Task<AuthorizationTestHost> StartAsync(
        FakeAuthenticatorEndpoint authenticator,
        Action<Dictionary<string, string?>>? configureSettings = null)
    {
        var logs = new RecordingLoggerProvider();
        var host = Build(authenticator, logs, configureSettings);

        await host.StartAsync();

        return new AuthorizationTestHost(host, authenticator, logs);
    }

    /// <summary>Barındırıcıyı kurar ama başlatmaz; fail-fast senaryosu için kullanılır.</summary>
    public static IHost Build(
        FakeAuthenticatorEndpoint authenticator,
        RecordingLoggerProvider logs,
        Action<Dictionary<string, string?>>? configureSettings = null)
    {
        var settings = DefaultSettings();
        configureSettings?.Invoke(settings);

        return new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(settings))
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(logs);
                })
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddControllers()
                        .AddApplicationPart(typeof(ProtectedController).Assembly);

                    services
                        .AddAuthentication(TestAuthenticationHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.SchemeName,
                            configureOptions: null);

                    services.AddAuthorization();

                    // Test edilen tek satirlik kurulum.
                    services.AddAuthenticatorAuthorization(context.Configuration);

                    // Uzak uc sahte handler ile karsilanir; named client'in geri kalani degismez.
                    services
                        .AddHttpClient(AuthenticatorHttpClientNames.Decision)
                        .ConfigurePrimaryHttpMessageHandler(() => authenticator);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
                }))
            .Build();
    }

    /// <summary>Korunan action'ın adresi.</summary>
    public static string ProtectedRoute => $"/{ProtectedController.RoutePrefix}/{ProtectedController.ManageRoute}";

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();

        await _host.StopAsync();

        _host.Dispose();
    }

    // Gecerli ve testler icin hizli bir varsayilan yapilandirma uretir.
    private static Dictionary<string, string?> DefaultSettings() => new()
    {
        [$"{AuthenticatorClientOptions.SectionName}:BaseUrl"] = "https://authenticator.test",
        [$"{AuthenticatorClientOptions.SectionName}:Timeout"] = "00:00:00.300",
        [$"{AuthenticatorClientOptions.SectionName}:RetryAttempts"] = "1",
        [$"{AuthenticatorClientOptions.SectionName}:RetryDelay"] = "00:00:00"
    };
}
