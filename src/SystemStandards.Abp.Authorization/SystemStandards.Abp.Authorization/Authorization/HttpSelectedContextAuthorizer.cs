using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemStandards.Abp.Authorization.Context;
using SystemStandards.Abp.Authorization.Http;
using SystemStandards.Abp.Authorization.Options;
using SystemStandards.Authorization.Contracts.Contracts;
using SystemStandards.Authorization.Contracts.FailureCodes;
using SystemStandards.Authorization.Contracts.Headers;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Karar sorusunu authorization server'ın HTTP ucuna taşıyan authorizer.
/// Dayanıklılık ve bağlantı yönetimi <c>IHttpClientFactory</c> + standart resilience handler'a aittir;
/// burada elle retry, circuit breaker veya cache yazılmaz.
/// Her hata yolu reddeder: hiçbir dal "izin ver"e düşmez.
/// </summary>
public sealed class HttpSelectedContextAuthorizer : ISelectedContextAuthorizer
{
    // Yanit gövdesi camelCase gelir; buyuk/kucuk harf duyarsizligi zarf bicimi degisse de okumayi ayakta tutar.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISelectedContextAccessor _selectedContextAccessor;
    private readonly IOptions<AuthenticatorClientOptions> _options;
    private readonly ILogger<HttpSelectedContextAuthorizer> _logger;

    /// <summary>Bağımlılıkları alır.</summary>
    public HttpSelectedContextAuthorizer(
        IHttpClientFactory httpClientFactory,
        ISelectedContextAccessor selectedContextAccessor,
        IOptions<AuthenticatorClientOptions> options,
        ILogger<HttpSelectedContextAuthorizer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _selectedContextAccessor = selectedContextAccessor;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SelectedContextDecision> AuthorizeAsync(
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var resolution = _selectedContextAccessor.Resolve();
        if (!resolution.IsResolved)
        {
            // Baglam okunamadiysa sorulacak bir sey yoktur; uzak cagri yapilmaz.
            _logger.LogWarning(
                "Authorization reddedildi: baglam okunamadi. Operasyon: {OperationName}, Gerekce: {FailureCode}",
                operationName,
                resolution.FailureCode);

            return SelectedContextDecision.Denied(resolution.FailureCode!);
        }

        try
        {
            return await RequestDecisionAsync(operationName, resolution.Context!, cancellationToken);
        }
        catch (Exception exception)
        {
            /* Fail-closed: tasima, timeout, DNS, TLS ve ayristirma hatalarinin hepsi ayni sonuca varir.
             * Exception turune gore dallanmak, gozden kacan bir turun "izin ver"e dusme riskini dogurur. */
            _logger.LogError(
                exception,
                "Authorization reddedildi: karar kaynagina ulasilamadi. Operasyon: {OperationName}",
                operationName);

            return SelectedContextDecision.Denied(
                SelectedContextFailureCodes.SelectedContext.AuthorizationSourceUnavailable);
        }
    }

    // Karar ucuna istegi kurar, gonderir ve yaniti karara cevirir.
    private async Task<SelectedContextDecision> RequestDecisionAsync(
        string operationName,
        SelectedContext context,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(AuthenticatorHttpClientNames.Decision);

        using var request = BuildRequest(operationName, context);
        using var response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Authorization reddedildi: karar ucu {StatusCode} dondu. Operasyon: {OperationName}",
                (int)response.StatusCode,
                operationName);

            return SelectedContextDecision.Denied(
                SelectedContextFailureCodes.SelectedContext.AuthorizationSourceUnavailable);
        }

        var envelope = await response.Content.ReadFromJsonAsync<AuthorizationResultEnvelopeContract>(
            SerializerOptions,
            cancellationToken);

        var decision = envelope?.Decision;
        if (envelope is null || !envelope.IsSucceeded || decision is null)
        {
            _logger.LogError(
                "Authorization reddedildi: karar govdesi okunamadi. Operasyon: {OperationName}",
                operationName);

            return SelectedContextDecision.Denied(
                SelectedContextFailureCodes.SelectedContext.AuthorizationSourceUnavailable);
        }

        if (!decision.IsAllowed)
        {
            // Ret bir hata degildir; gerekcesi kararli kodla loglanir.
            _logger.LogWarning(
                "Authorization reddedildi. Operasyon: {OperationName}, Gerekce: {FailureCode}",
                operationName,
                decision.FailureCode);

            return SelectedContextDecision.Denied(
                decision.FailureCode
                ?? SelectedContextFailureCodes.SelectedContext.OperationPermissionRequired);
        }

        return SelectedContextDecision.FromContract(decision);
    }

    // Cagiranin bearer token'ini ve privileged header'larini uzak istege tasir.
    private HttpRequestMessage BuildRequest(string operationName, SelectedContext context)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.Value.AuthorizePath)
        {
            Content = JsonContent.Create(
                new AuthorizationRequestContract { OperationName = operationName },
                options: SerializerOptions)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

        if (context.PrivilegedReason is not null)
        {
            request.Headers.TryAddWithoutValidation(
                SelectedContextHeaderNames.PrivilegedReason,
                context.PrivilegedReason);
        }

        if (context.CorrelationId is not null)
        {
            request.Headers.TryAddWithoutValidation(
                SelectedContextHeaderNames.CorrelationId,
                context.CorrelationId);
        }

        return request;
    }
}
