using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SystemStandards.Abp.Authorization.Tests.Fakes;

/// <summary>
/// Resource server'ın token doğrulamasının yerine geçen test kimlik doğrulayıcısı.
/// Bearer token'ı çözüp claim'leri principal'a yazar; token yoksa kimlik de yoktur.
/// </summary>
internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Test şemasının adı.</summary>
    public const string SchemeName = "TestBearer";

    // Authorization header'inda beklenen sema.
    private const string BearerPrefix = "Bearer ";

    /// <summary>Bağımlılıkları alır.</summary>
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = TestAccessToken.Decode(header[BearerPrefix.Length..].Trim());
        if (claims is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Token cozulemedi."));
        }

        var identity = new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Key, claim.Value)),
            SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
