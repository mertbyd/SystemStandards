using FluentAssertions;
using SystemStandards.Extensions;
using SystemStandards.Results;
using Xunit;

namespace SystemStandards.Tests;

/// <summary>
/// Result monadik extension'lar için birim testleri.
/// </summary>
public class ResultExtensionsTests
{
    [Fact]
    public void Map_Should_Transform_Value_When_Success()
    {
        var result = Result<int>.Success(42);

        var mapped = result.Map(x => x.ToString());

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public void Map_Should_Skip_On_Failure()
    {
        var result = Result<int>.NotFound("Yok");
        var mapperCalled = false;

        var mapped = result.Map(x =>
        {
            mapperCalled = true;
            return x.ToString();
        });

        mapperCalled.Should().BeFalse();
        mapped.IsSuccess.Should().BeFalse();
        mapped.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task MapAsync_Should_Transform_Value_When_Success()
    {
        var result = Result<int>.Success(10);

        var mapped = await result.MapAsync(x => System.Threading.Tasks.Task.FromResult(x * 2));

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(20);
    }

    [Fact]
    public void Filter_Should_Return_Result_When_Predicate_True()
    {
        var result = Result<int>.Success(5);

        var filtered = result.Filter(x => x > 0);

        filtered.IsSuccess.Should().BeTrue();
        filtered.Value.Should().Be(5);
    }

    [Fact]
    public void Filter_Should_Return_NotFound_When_Predicate_False()
    {
        var result = Result<int>.Success(-1);

        var filtered = result.Filter(x => x > 0, "Negatif değer");

        filtered.IsSuccess.Should().BeFalse();
        filtered.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public void Bind_Should_Chain_Results()
    {
        var result = Result<int>.Success(5);

        var bound = result.Bind(x => Result<string>.Success($"Value: {x}"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("Value: 5");
    }

    [Fact]
    public void Bind_Should_Propagate_Failure()
    {
        var result = Result<int>.Error("Initial error");

        var bound = result.Bind(x => Result<string>.Success($"Value: {x}"));

        bound.IsSuccess.Should().BeFalse();
        bound.Status.Should().Be(ResultStatus.Error);
    }
}
