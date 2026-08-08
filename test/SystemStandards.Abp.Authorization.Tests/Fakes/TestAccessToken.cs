using System.Text;
using System.Text.Json;
using SystemStandards.Authorization.Contracts.Claims;

namespace SystemStandards.Abp.Authorization.Tests.Fakes;

/// <summary>
/// Testlerde kullanılan sahte access token.
/// Token, claim kümesinin base64url kodlanmış hâlidir: test kimlik doğrulayıcısı bunu çözer,
/// sarmalayıcı ise token'a hiç bakmadan olduğu gibi ileri taşır.
/// </summary>
internal static class TestAccessToken
{
    /// <summary>Testlerde kullanılan kiracı kimliği.</summary>
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Testlerde kullanılan organizasyon birimi kimliği.</summary>
    public static readonly Guid OrganizationUnitId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>Testlerde kullanılan application scope kimliği.</summary>
    public static readonly Guid ApplicationScopeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>Testlerde beklenen token scope'u.</summary>
    public const string Scope = "AuthenticatorApi";

    /// <summary>Bütün zorunlu claim'leri taşıyan geçerli bir claim kümesi üretir.</summary>
    public static Dictionary<string, string> ValidClaims() => new()
    {
        [SelectedContextClaimNames.Subject] = "44444444-4444-4444-4444-444444444444",
        [SelectedContextClaimNames.ActorKind] = "User",
        [SelectedContextClaimNames.Scope] = Scope,
        [SelectedContextClaimNames.TenantId] = TenantId.ToString(),
        [SelectedContextClaimNames.OrganizationUnitId] = OrganizationUnitId.ToString(),
        [SelectedContextClaimNames.ApplicationScopeId] = ApplicationScopeId.ToString()
    };

    /// <summary>Claim kümesini token dizesine çevirir.</summary>
    public static string Encode(IReadOnlyDictionary<string, string> claims)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(claims);

        return Convert.ToBase64String(payload);
    }

    /// <summary>Token dizesini claim kümesine çözer; çözülemezse null döner.</summary>
    public static IReadOnlyDictionary<string, string>? Decode(string token)
    {
        try
        {
            var payload = Convert.FromBase64String(token);

            return JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(payload));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Geçerli claim kümesinden bir token üretir.</summary>
    public static string Valid() => Encode(ValidClaims());
}
