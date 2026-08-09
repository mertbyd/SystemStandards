using FluentAssertions;
using SystemStandards.Authorization.Contracts.FailureCodes;
using Xunit;

namespace SystemStandards.Authorization.Contracts.Tests;

/// <summary>
/// Kabul 14 — ret gerekçe kodları authorization server'ın ürettikleriyle birebir aynıdır.
/// Kodlar literal değerlerle karşılaştırılır ve prefix başına eksiksiz numara dizisi aranır:
/// tek bir kayan numara iki taraftaki gerekçeleri sessizce kaydırır.
/// </summary>
public class SelectedContextFailureCodesTests
{
    [Theory]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.NotFound), "Authenticator.SelectedContext:00001")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.TenantInactive), "Authenticator.SelectedContext:00002")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.OrganizationUnitInactive), "Authenticator.SelectedContext:00003")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.ScopeInactive), "Authenticator.SelectedContext:00004")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.MembershipRequired), "Authenticator.SelectedContext:00005")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.RoleAssignmentRequired), "Authenticator.SelectedContext:00006")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.OperationPermissionRequired), "Authenticator.SelectedContext:00007")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.HostAdministrationNotAllowed), "Authenticator.SelectedContext:00008")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.ReasonRequired), "Authenticator.SelectedContext:00009")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.CorrelationRequired), "Authenticator.SelectedContext:00010")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.AuthorizationSourceUnavailable), "Authenticator.SelectedContext:00011")]
    [InlineData(nameof(SelectedContextFailureCodes.SelectedContext.ContextAlreadyExists), "Authenticator.SelectedContext:00012")]
    public void Selected_context_failure_codes_match_the_authorization_server(string constantName, string expectedValue)
    {
        ReadConstant(typeof(SelectedContextFailureCodes.SelectedContext), constantName).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.MissingActor), "Authenticator.AuthContext:00001")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.UnsupportedActor), "Authenticator.AuthContext:00002")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.AmbiguousActor), "Authenticator.AuthContext:00003")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.MissingApplicationScope), "Authenticator.AuthContext:00004")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.MissingTenant), "Authenticator.AuthContext:00005")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.MalformedTenant), "Authenticator.AuthContext:00006")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.MissingOrganizationUnit), "Authenticator.AuthContext:00007")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.MalformedOrganizationUnit), "Authenticator.AuthContext:00008")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.InvalidScope), "Authenticator.AuthContext:00009")]
    [InlineData(nameof(SelectedContextFailureCodes.AuthContext.Rejected), "Authenticator.AuthContext:00010")]
    public void Auth_context_failure_codes_match_the_authorization_server(string constantName, string expectedValue)
    {
        ReadConstant(typeof(SelectedContextFailureCodes.AuthContext), constantName).Should().Be(expectedValue);
    }

    [Fact]
    public void Selected_context_prefix_carries_an_unbroken_numbering_from_one_to_twelve()
    {
        CodesOf(typeof(SelectedContextFailureCodes.SelectedContext), "Authenticator.SelectedContext")
            .Should()
            .BeEquivalentTo(NumberedCodes("Authenticator.SelectedContext", 12));
    }

    [Fact]
    public void Auth_context_prefix_carries_an_unbroken_numbering_from_one_to_ten()
    {
        CodesOf(typeof(SelectedContextFailureCodes.AuthContext), "Authenticator.AuthContext")
            .Should()
            .BeEquivalentTo(NumberedCodes("Authenticator.AuthContext", 10));
    }

    // Sabitin degerini adiyla okur.
    private static string ReadConstant(Type owner, string constantName)
        => (string)owner.GetField(constantName)!.GetRawConstantValue()!;

    // Prefix sabitini disarida birakip yalniz numarali kodlari dondurur.
    private static IEnumerable<string> CodesOf(Type owner, string prefix)
        => owner.GetFields()
            .Where(field => field is {IsLiteral: true, IsInitOnly: false} && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .Where(value => value != prefix);

    // Beklenen numarali kod dizisini uretir.
    private static IEnumerable<string> NumberedCodes(string prefix, int count)
        => Enumerable.Range(1, count).Select(number => $"{prefix}:{number:D5}");
}
