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
using SystemStandards.Results;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Karar sorusunu authorization server'ın HTTP ucuna taşıyan authorizer.
/// Dayanıklılık ve bağlantı yönetimi <c>IHttpClientFactory</c> + standart resilience handler'a aittir;
/// burada elle retry, circuit breaker veya cache yazılmaz.
/// Her hata yolu reddeder: hiçbir dal "izin ver"e düşmez.
/// </summary>
public sealed class HttpSelectedContextAuthorizer : ISelectedContextAuthorizer
{
    // Sunucu ve istemci ayni Result<T> tel bicimini kullanir; ikinci bir response DTO tanimlanmaz.
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly string ResultSuccessPropertyName =
        JsonNamingPolicy.CamelCase.ConvertName(nameof(Result.IsSuccess));
    private static readonly string ResultStatusPropertyName =
        JsonNamingPolicy.CamelCase.ConvertName(nameof(Result.Status));
    private static readonly string ResultValuePropertyName =
        JsonNamingPolicy.CamelCase.ConvertName(nameof(Result<AuthorizationDecisionContract>.Value));

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

        var result = await ReadDecisionResultAsync(response.Content, cancellationToken);

        var decision = result?.Value;
        if (result is null || !result.IsSuccess || decision is null)
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

    // Result<T> tel bicimini dogrudan okur; zarf gecersizse karar uretmez.
    private static async Task<Result<AuthorizationDecisionContract>?> ReadDecisionResultAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (!TryGetProperty(root, ResultSuccessPropertyName, out var success)
            || success.ValueKind != JsonValueKind.True
            || !TryGetProperty(root, ResultStatusPropertyName, out var status)
            || status.Deserialize<ResultStatus>(SerializerOptions) != ResultStatus.Ok
            || !TryGetProperty(root, ResultValuePropertyName, out var value))
        {
            return null;
        }

        var decision = value.Deserialize<AuthorizationDecisionContract>(SerializerOptions);
        return decision is null
            ? null
            : Result<AuthorizationDecisionContract>.Success(decision);
    }

    // JSON adlandirma politikasindan bagimsiz olarak Result property adini bulur.
    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = property.Value;
            return true;
        }

        value = default;
        return false;
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
