namespace SystemStandards.Abp.Authorization.Tests.Fakes;

/// <summary>
/// Sahte authenticator ucuna ulaşan bir isteğin kaydı.
/// İstek nesnesi pipeline tarafından dispose edildiği için içerik okunup burada saklanır.
/// </summary>
internal sealed class RecordedAuthorizationRequest
{
    /// <summary>İsteğin HTTP metodu.</summary>
    public required string Method { get; init; }

    /// <summary>İsteğin tam adresi.</summary>
    public required Uri RequestUri { get; init; }

    /// <summary>Authorization header'ının şeması (örn. Bearer).</summary>
    public string? AuthorizationScheme { get; init; }

    /// <summary>Authorization header'ının taşıdığı token.</summary>
    public string? AuthorizationParameter { get; init; }

    /// <summary>İstek header'ları.</summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>İstek gövdesinin ham hali.</summary>
    public required string Body { get; init; }

    /// <summary>Giden isteği kayda çevirir.</summary>
    public static async Task<RecordedAuthorizationRequest> CaptureAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return new RecordedAuthorizationRequest
        {
            Method = request.Method.Method,
            RequestUri = request.RequestUri!,
            AuthorizationScheme = request.Headers.Authorization?.Scheme,
            AuthorizationParameter = request.Headers.Authorization?.Parameter,
            Headers = request.Headers.ToDictionary(
                header => header.Key,
                header => string.Join(",", header.Value),
                StringComparer.OrdinalIgnoreCase),
            Body = body
        };
    }
}
