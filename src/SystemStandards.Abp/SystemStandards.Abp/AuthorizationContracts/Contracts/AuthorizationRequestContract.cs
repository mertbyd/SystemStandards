using System.Text.Json.Serialization;

namespace SystemStandards.Authorization.Contracts.Contracts;

/// <summary>
/// Karar ucuna gönderilen istek gövdesi.
/// Gövde yalnız operasyon adı taşır; actor ve bağlam token'dan okunur, istemciden kabul edilmez.
/// </summary>
public sealed class AuthorizationRequestContract
{
    /// <summary>İzni sorulan operasyonun kararlı adı (permission adı).</summary>
    [JsonPropertyName("operationName")]
    public string OperationName { get; init; } = string.Empty;
}
