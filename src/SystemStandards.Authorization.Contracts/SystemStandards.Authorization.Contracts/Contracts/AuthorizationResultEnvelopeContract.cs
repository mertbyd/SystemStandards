using System.Text.Json.Serialization;

namespace SystemStandards.Authorization.Contracts.Contracts;

/// <summary>
/// Karar yanıtının SystemStandards <c>Result&lt;T&gt;</c> zarfı.
/// Zarf iki biçimde tel üstüne çıkabilir: <c>Result&lt;T&gt;</c> doğrudan serialize edildiğinde
/// yük <c>value</c> altında, <c>ToActionResult</c> zarfı devredeyse <c>data</c> altında durur.
/// İki biçim de okunur; ikisi de yoksa karar yoktur ve çağrı fail-closed reddedilir.
/// </summary>
public sealed class AuthorizationResultEnvelopeContract
{
    /// <summary><c>Result&lt;T&gt;</c> doğrudan serialize edildiğinde kararın durduğu alan.</summary>
    [JsonPropertyName("value")]
    public AuthorizationDecisionContract? Value { get; init; }

    /// <summary><c>ToActionResult</c> zarfı devredeyken kararın durduğu alan.</summary>
    [JsonPropertyName("data")]
    public AuthorizationDecisionContract? Data { get; init; }

    /// <summary><c>Result&lt;T&gt;</c> doğrudan serialize edildiğinde başarı bayrağı.</summary>
    [JsonPropertyName("isSuccess")]
    public bool? IsSuccess { get; init; }

    /// <summary><c>ToActionResult</c> zarfı devredeyken başarı bayrağı.</summary>
    [JsonPropertyName("success")]
    public bool? Success { get; init; }

    /// <summary>Zarfın hangi biçimde geldiğinden bağımsız olarak taşınan karar.</summary>
    [JsonIgnore]
    public AuthorizationDecisionContract? Decision => Value ?? Data;

    /// <summary>
    /// Zarf başarılı bir sonuç taşıyor mu. İki bayrak da yoksa false döner:
    /// belirsizlik "izin ver"e değil "reddet"e düşer.
    /// </summary>
    [JsonIgnore]
    public bool IsSucceeded => Success ?? IsSuccess ?? false;
}
