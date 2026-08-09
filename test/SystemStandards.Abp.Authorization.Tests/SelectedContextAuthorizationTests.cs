using System.Net;
using System.Text.Json;
using FluentAssertions;
using SystemStandards.Abp.Authorization.Tests.App;
using SystemStandards.Abp.Authorization.Tests.Fakes;
using SystemStandards.Authorization.Contracts.Claims;
using SystemStandards.Authorization.Contracts.Contracts;
using SystemStandards.Authorization.Contracts.Endpoints;
using SystemStandards.Authorization.Contracts.FailureCodes;
using SystemStandards.Authorization.Contracts.Headers;
using Xunit;

namespace SystemStandards.Abp.Authorization.Tests;

/// <summary>
/// Kabul 1-11 — korunan bir action'ın karar ucuyla ilişkisi.
/// Pozitif yolda istek geçer; negatif yolların hepsi reddeder ve hiçbiri "izin ver"e düşmez.
/// </summary>
public class SelectedContextAuthorizationTests
{
    // ==================== POZITIF ====================

    // Kabul 1: uc izin verdiginde korunan action calisir.
    [Fact]
    public async Task An_allowing_decision_lets_the_protected_action_run()
    {
        var authenticator = new FakeAuthenticatorEndpoint();
        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var response = await host.Client.SendAsync(BuildRequest(TestAccessToken.Valid()));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain(TestOperationNames.Manage);
    }

    // Kabul 2: cagiranin bearer token'i uzak istege aynen iletilir.
    [Fact]
    public async Task The_callers_bearer_token_is_forwarded_verbatim()
    {
        var authenticator = new FakeAuthenticatorEndpoint();
        await using var host = await AuthorizationTestHost.StartAsync(authenticator);
        var token = TestAccessToken.Valid();

        await host.Client.SendAsync(BuildRequest(token));

        var remoteCall = authenticator.Requests.Should().ContainSingle().Subject;
        remoteCall.AuthorizationScheme.Should().Be("Bearer");
        remoteCall.AuthorizationParameter.Should().Be(token);
    }

    // Kabul 3: privileged header'lar varsa uzak istege iletilir.
    [Fact]
    public async Task Privileged_headers_are_forwarded_when_present()
    {
        const string reason = "Kiraci tasima operasyonu";
        const string correlationId = "b7a1f0c2-9d2e-4c8a-9f11-6f0a2c3d4e5f";

        var authenticator = new FakeAuthenticatorEndpoint();
        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var request = BuildRequest(TestAccessToken.Valid());
        request.Headers.TryAddWithoutValidation(SelectedContextHeaderNames.PrivilegedReason, reason);
        request.Headers.TryAddWithoutValidation(SelectedContextHeaderNames.CorrelationId, correlationId);

        await host.Client.SendAsync(request);

        var remoteCall = authenticator.Requests.Should().ContainSingle().Subject;
        remoteCall.Headers[SelectedContextHeaderNames.PrivilegedReason].Should().Be(reason);
        remoteCall.Headers[SelectedContextHeaderNames.CorrelationId].Should().Be(correlationId);
    }

    // Kabul 4: attribute'teki permission adi govdedeki operationName olarak gider.
    [Fact]
    public async Task The_permission_name_travels_as_the_operation_name_in_the_body()
    {
        var authenticator = new FakeAuthenticatorEndpoint();
        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        await host.Client.SendAsync(BuildRequest(TestAccessToken.Valid()));

        var remoteCall = authenticator.Requests.Should().ContainSingle().Subject;
        remoteCall.Method.Should().Be(HttpMethod.Post.Method);
        remoteCall.RequestUri.AbsolutePath.Should().Be($"/{AuthenticatorEndpointPaths.AuthorizeSelectedContext}");

        var body = JsonSerializer.Deserialize<AuthorizationRequestContract>(remoteCall.Body);
        body!.OperationName.Should().Be(TestOperationNames.Manage);
    }

    // ==================== NEGATIF ====================

    // Kabul 5: token yoksa cevap 401'dir ve uzak cagri hic yapilmaz.
    [Fact]
    public async Task A_request_without_a_token_is_challenged_without_a_remote_call()
    {
        var authenticator = new FakeAuthenticatorEndpoint();
        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var response = await host.Client.SendAsync(BuildRequest(token: null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        authenticator.Requests.Should().BeEmpty();
    }

    // Kabul 6: actor_kind claim'i yoksa reddedilir ve uzak cagri yapilmaz.
    [Fact]
    public async Task A_missing_actor_kind_claim_is_rejected_without_a_remote_call()
    {
        var claims = TestAccessToken.ValidClaims();
        claims.Remove(SelectedContextClaimNames.ActorKind);

        var authenticator = new FakeAuthenticatorEndpoint();
        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var response = await host.Client.SendAsync(BuildRequest(TestAccessToken.Encode(claims)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        authenticator.Requests.Should().BeEmpty();
        host.Logs.Messages.Should().Contain(message =>
            message.Contains(SelectedContextFailureCodes.AuthContext.MissingActor));
    }

    // Kabul 7: tenant_id bozuksa reddedilir ve uzak cagri yapilmaz.
    [Fact]
    public async Task A_malformed_tenant_claim_is_rejected_without_a_remote_call()
    {
        var claims = TestAccessToken.ValidClaims();
        claims[SelectedContextClaimNames.TenantId] = "bu-bir-kimlik-degil";

        var authenticator = new FakeAuthenticatorEndpoint();
        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var response = await host.Client.SendAsync(BuildRequest(TestAccessToken.Encode(claims)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        authenticator.Requests.Should().BeEmpty();
        host.Logs.Messages.Should().Contain(message =>
            message.Contains(SelectedContextFailureCodes.AuthContext.MalformedTenant));
    }

    // Kabul 8: uc reddettiginde cevap 403'tur ve gerekce kodu loglanir.
    [Fact]
    public async Task A_denying_decision_is_rejected_and_its_failure_code_is_logged()
    {
        var authenticator = new FakeAuthenticatorEndpoint
        {
            Respond = _ => FakeAuthenticatorEndpoint.Denied(
                SelectedContextFailureCodes.SelectedContext.OperationPermissionRequired)
        };

        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var response = await host.Client.SendAsync(BuildRequest(TestAccessToken.Valid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        authenticator.Requests.Should().ContainSingle();
        host.Logs.Messages.Should().Contain(message =>
            message.Contains(SelectedContextFailureCodes.SelectedContext.OperationPermissionRequired));
    }

    // Kabul 9: uc 500 dondugunde reddedilir.
    [Fact]
    public async Task A_server_error_from_the_decision_endpoint_is_rejected()
    {
        var authenticator = new FakeAuthenticatorEndpoint
        {
            Respond = _ => FakeAuthenticatorEndpoint.Status(HttpStatusCode.InternalServerError)
        };

        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var response = await host.Client.SendAsync(BuildRequest(TestAccessToken.Valid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        authenticator.Requests.Should().NotBeEmpty();
        host.Logs.Messages.Should().Contain(message => message.Contains("500"));
    }

    // Kabul 10: uc zamaninda cevap vermezse reddedilir.
    [Fact]
    public async Task A_timeout_on_the_decision_endpoint_is_rejected()
    {
        var authenticator = new FakeAuthenticatorEndpoint
        {
            Delay = TimeSpan.FromSeconds(30)
        };

        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var response = await host.Client.SendAsync(BuildRequest(TestAccessToken.Valid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cagri gerçekten cikti ama karar donmedi: ret taşıma katmanindan geliyor, claim'lerden degil.
        authenticator.Requests.Should().NotBeEmpty();
    }

    // Kabul 11: govde ayristirilamiyorsa reddedilir.
    [Fact]
    public async Task An_unparsable_decision_body_is_rejected()
    {
        var authenticator = new FakeAuthenticatorEndpoint
        {
            Respond = _ => FakeAuthenticatorEndpoint.Unparsable()
        };

        await using var host = await AuthorizationTestHost.StartAsync(authenticator);

        var response = await host.Client.SendAsync(BuildRequest(TestAccessToken.Valid()));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        authenticator.Requests.Should().NotBeEmpty();
    }

    // Korunan action'a gidecek istegi kurar.
    private static HttpRequestMessage BuildRequest(string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, AuthorizationTestHost.ProtectedRoute);

        if (token is not null)
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        }

        return request;
    }
}
