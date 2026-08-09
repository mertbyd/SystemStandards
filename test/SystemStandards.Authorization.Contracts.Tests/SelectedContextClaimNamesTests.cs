using FluentAssertions;
using SystemStandards.Authorization.Contracts.Claims;
using SystemStandards.Authorization.Contracts.Headers;
using Xunit;

namespace SystemStandards.Authorization.Contracts.Tests;

/// <summary>
/// Kabul 13 — claim ve header adları authorization server'ın sözleşmesiyle birebir aynıdır.
/// Karşılaştırma bilerek literal değerlerle yapılır: bu paket sözleşmenin kendisidir,
/// kendi sabitiyle kendini doğrulayan bir test hiçbir sapmayı yakalamaz.
/// </summary>
public class SelectedContextClaimNamesTests
{
    [Theory]
    [InlineData(nameof(SelectedContextClaimNames.Subject), "sub")]
    [InlineData(nameof(SelectedContextClaimNames.ClientId), "client_id")]
    [InlineData(nameof(SelectedContextClaimNames.Scope), "scope")]
    [InlineData(nameof(SelectedContextClaimNames.ActorKind), "actor_kind")]
    [InlineData(nameof(SelectedContextClaimNames.TenantId), "tenant_id")]
    [InlineData(nameof(SelectedContextClaimNames.OrganizationUnitId), "organization_unit_id")]
    [InlineData(nameof(SelectedContextClaimNames.ApplicationScopeId), "application_scope_id")]
    public void Claim_names_match_the_authorization_server_contract(string constantName, string expectedValue)
    {
        ReadConstant(typeof(SelectedContextClaimNames), constantName).Should().Be(expectedValue);
    }

    [Fact]
    public void Claim_name_set_carries_exactly_the_agreed_seven_claims()
    {
        ConstantValues(typeof(SelectedContextClaimNames))
            .Should()
            .BeEquivalentTo(
            [
                "sub",
                "client_id",
                "scope",
                "actor_kind",
                "tenant_id",
                "organization_unit_id",
                "application_scope_id"
            ]);
    }

    [Theory]
    [InlineData(nameof(SelectedContextHeaderNames.PrivilegedReason), "X-Authenticator-Privileged-Reason")]
    [InlineData(nameof(SelectedContextHeaderNames.CorrelationId), "X-Authenticator-Correlation-Id")]
    public void Privileged_header_names_match_the_authorization_server_contract(string constantName, string expectedValue)
    {
        ReadConstant(typeof(SelectedContextHeaderNames), constantName).Should().Be(expectedValue);
    }

    // Sabitin degerini adiyla okur; testin yanlis sabiti okumasi mumkun olmasin diye reflection kullanilir.
    private static string ReadConstant(Type owner, string constantName)
        => (string)owner.GetField(constantName)!.GetRawConstantValue()!;

    // Tipteki butun public string sabitlerinin degerlerini dondurur.
    private static IEnumerable<string> ConstantValues(Type owner)
        => owner.GetFields()
            .Where(field => field is {IsLiteral: true, IsInitOnly: false} && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);
}
