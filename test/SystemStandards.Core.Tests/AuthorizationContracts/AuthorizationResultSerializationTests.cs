using System.Text.Json;
using FluentAssertions;
using SystemStandards.Authorization.Contracts.Contracts;
using SystemStandards.Results;
using Xunit;

namespace SystemStandards.Authorization.Contracts.Tests;

/// <summary>
/// Authorization kararinin ayri bir zarf yazilmadan SystemStandards Result ile tasindigini dogrular.
/// </summary>
public class AuthorizationResultSerializationTests
{
    /// <summary>Result zarfini JSON turunda ayni karar ve durumla geri kurar.</summary>
    [Fact]
    public void Result_round_trip_preserves_the_authorization_decision()
    {
        var expected = Result<AuthorizationDecisionContract>.Success(new AuthorizationDecisionContract
        {
            IsAllowed = true,
            ContextVersion = 42,
            ApplicationScopeLevelCode = "OrganizationUnit",
            RequiresPrivilegedAudit = true
        });

        var json = JsonSerializer.Serialize(expected);
        var actual = JsonSerializer.Deserialize<Result<AuthorizationDecisionContract>>(json);

        actual.Should().NotBeNull();
        actual!.IsSuccess.Should().BeTrue();
        actual.Status.Should().Be(ResultStatus.Ok);
        actual.Value.Should().BeEquivalentTo(expected.Value);
    }
}
