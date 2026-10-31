using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using SystemStandards.Abp.Authorization.Authorization;
using SystemStandards.Abp.Authorization.Tests.App;
using Xunit;

namespace SystemStandards.Abp.Authorization.Tests;

/// <summary>
/// Attribute'ün framework ile buluştuğu nokta.
/// ASP.NET Core authorization middleware'i ve ABP'nin method invocation authorization'ı, authorize
/// verisini <c>AuthorizationPolicy.CombineAsync</c> ile policy'ye çevirir ve policy'yi <b>adıyla</b>
/// sağlayıcıdan ister. Buradaki testler o yolun requirement üretmeye devam ettiğini kilitler:
/// üretmezse attribute application service metotlarında sessizce etkisiz kalır.
/// </summary>
public class RequireSelectedContextAttributeTests
{
    [Fact]
    public void The_attribute_is_authorize_data_so_the_abp_interceptor_picks_it_up()
    {
        typeof(RequireSelectedContextAttribute).Should().BeAssignableTo<AuthorizeAttribute>();
    }

    [Fact]
    public void The_attribute_carries_the_operation_name_as_its_policy_name()
    {
        var attribute = new RequireSelectedContextAttribute(TestOperationNames.Manage);

        attribute.Policy.Should().Be(SelectedContextPolicyNames.Prefix + TestOperationNames.Manage);
        attribute.OperationName.Should().Be(TestOperationNames.Manage);
    }

    [Fact]
    public async Task Combining_the_attribute_produces_the_selected_context_requirement()
    {
        var attribute = new RequireSelectedContextAttribute(TestOperationNames.Manage);

        var policy = await AuthorizationPolicy.CombineAsync(BuildPolicyProvider(), [attribute]);

        policy.Should().NotBeNull();

        var requirement = policy!.Requirements.OfType<SelectedContextRequirement>().Should().ContainSingle().Subject;
        requirement.OperationName.Should().Be(TestOperationNames.Manage);
    }

    [Fact]
    public async Task Combining_the_attribute_keeps_anonymous_requests_out()
    {
        var attribute = new RequireSelectedContextAttribute(TestOperationNames.Manage);

        var policy = await AuthorizationPolicy.CombineAsync(BuildPolicyProvider(), [attribute]);

        policy!.Requirements.Should().ContainItemsAssignableTo<DenyAnonymousAuthorizationRequirement>();
    }

    [Fact]
    public async Task A_policy_name_from_another_owner_is_handed_to_the_inner_provider()
    {
        var inner = new StubPolicyProvider();
        var provider = new SelectedContextAuthorizationPolicyProvider(inner);

        var policy = await provider.GetPolicyAsync("SomeoneElse.Policy");

        policy.Should().BeSameAs(StubPolicyProvider.KnownPolicy);
        inner.RequestedPolicyNames.Should().Equal("SomeoneElse.Policy");
    }

    [Fact]
    public void An_empty_operation_name_is_refused_at_construction()
    {
        var construct = () => new RequireSelectedContextAttribute("   ");

        construct.Should().Throw<ArgumentException>();
    }

    // Sarmalayicinin cozdugu saglayici; testlerde ic saglayici olarak da kullanilir.
    private static IAuthorizationPolicyProvider BuildPolicyProvider()
        => new SelectedContextAuthorizationPolicyProvider(new StubPolicyProvider());

    // Devredilen cagrilari kaydeden ic saglayici.
    private sealed class StubPolicyProvider : IAuthorizationPolicyProvider
    {
        public static readonly AuthorizationPolicy KnownPolicy =
            new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

        private readonly List<string> _requestedPolicyNames = [];

        public IReadOnlyList<string> RequestedPolicyNames => _requestedPolicyNames;

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => Task.FromResult(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => Task.FromResult<AuthorizationPolicy?>(null);

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            _requestedPolicyNames.Add(policyName);

            return Task.FromResult<AuthorizationPolicy?>(KnownPolicy);
        }
    }
}
