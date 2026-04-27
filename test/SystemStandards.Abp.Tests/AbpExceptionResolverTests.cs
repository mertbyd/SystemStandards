using FluentAssertions;
using NSubstitute;
using SystemStandards.Abp.ExceptionHandling;
using SystemStandards.Mapping;
using SystemStandards.Results;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;
using Xunit;

namespace SystemStandards.Abp.Tests;

/// <summary>
/// AbpExceptionToResultStatusResolver için birim testleri.
/// </summary>
public class AbpExceptionResolverTests
{
    private readonly IExceptionToResultStatusResolver _defaultResolver;
    private readonly AbpExceptionToResultStatusResolver _resolver;

    public AbpExceptionResolverTests()
    {
        _defaultResolver = Substitute.For<IExceptionToResultStatusResolver>();
        _defaultResolver.Resolve(Arg.Any<Exception>()).Returns(ResultStatus.Error);
        _resolver = new AbpExceptionToResultStatusResolver(_defaultResolver);
    }

    [Fact]
    public void EntityNotFoundException_Should_Map_To_NotFound()
    {
        var ex = new EntityNotFoundException(typeof(object), "1");

        var status = _resolver.Resolve(ex);

        status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public void AbpAuthorizationException_Should_Map_To_Forbidden()
    {
        var ex = new AbpAuthorizationException("Erişim yasaklı");

        var status = _resolver.Resolve(ex);

        status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public void AbpValidationException_Should_Map_To_Invalid()
    {
        var ex = new AbpValidationException("Doğrulama hatası");

        var status = _resolver.Resolve(ex);

        status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public void UnknownException_Should_Delegate_To_Default_Resolver()
    {
        var ex = new InvalidOperationException("Bilinmeyen hata");
        _defaultResolver.Resolve(ex).Returns(ResultStatus.Conflict);

        var status = _resolver.Resolve(ex);

        status.Should().Be(ResultStatus.Conflict);
        _defaultResolver.Received(1).Resolve(ex);
    }
}
