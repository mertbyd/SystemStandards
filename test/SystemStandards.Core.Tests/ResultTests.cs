using FluentAssertions;
using SystemStandards.Errors;
using SystemStandards.Mapping;
using SystemStandards.Results;
using Xunit;

namespace SystemStandards.Tests;

/// <summary>
/// Core Result sınıfları için temel birim testleri.
/// </summary>
public class ResultTests
{
    [Fact]
    public void Result_Should_Return_Ok_When_Success()
    {
        var result = Result.Success("İşlem başarılı");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(ResultStatus.Ok);
        result.SuccessMessage.Should().Be("İşlem başarılı");
    }

    [Fact]
    public void ResultT_Should_Return_Ok_When_Success()
    {
        var dto = new { Name = "Test" };
        var result = Result<object>.Success(dto);

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(ResultStatus.Ok);
        result.Value.Should().Be(dto);
    }

    [Fact]
    public void ResultT_Should_Map_Status_Correctly_For_NotFound()
    {
        var result = Result<string>.NotFound("Kaynak bulunamadı");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain("Kaynak bulunamadı");
    }

    [Fact]
    public void ResultT_Should_Map_Status_Correctly_For_Invalid()
    {
        var errors = new[]
        {
            new ValidationError("Name", "Ad boş olamaz", "REQUIRED"),
            new ValidationError("Email", "Email hatalı", "EMAIL_INVALID")
        };

        var result = Result<string>.Invalid(errors);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().HaveCount(2);
    }

    [Fact]
    public void ResultT_Implicit_Null_Should_Return_NotFound()
    {
        // null → NotFound (RFC 7231 anlamlı)
        Result<string?> result = (string?)null;

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public void ResultT_Implicit_Value_Should_Return_Success()
    {
        Result<string> result = "Hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Hello");
    }

    [Fact]
    public void Result_Error_With_ErrorList_Should_Include_CorrelationId()
    {
        var errorList = new ErrorList(["Hata 1", "Hata 2"], "correlation-123");

        var result = Result.Error(errorList);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.CorrelationId.Should().Be("correlation-123");
    }

    [Fact]
    public void Result_WithCorrelationId_Should_Chain()
    {
        var result = Result.Success()
            .WithCorrelationId("abc-123")
            .WithLocation("/api/test/1");

        result.CorrelationId.Should().Be("abc-123");
        result.Location.Should().Be("/api/test/1");
    }

    [Fact]
    public void ResultT_Created_Should_Have_Location()
    {
        var result = Result<string>.Created("data", "/api/products/1");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(ResultStatus.Created);
        result.Location.Should().Be("/api/products/1");
    }
}

/// <summary>
/// ResultStatusMap için birim testleri.
/// </summary>
public class ResultStatusMapTests
{
    [Fact]
    public void ResultStatusMap_Should_Have_Default_Mappings()
    {
        var map = new ResultStatusMap().AddDefaultMap();

        map.ContainsKey(ResultStatus.Ok).Should().BeTrue();
        map.ContainsKey(ResultStatus.NotFound).Should().BeTrue();
        map.ContainsKey(ResultStatus.Invalid).Should().BeTrue();
        map.ContainsKey(ResultStatus.Error).Should().BeTrue();
    }

    [Fact]
    public void ResultStatusMap_Should_Return_Correct_HttpCode()
    {
        var map = new ResultStatusMap().AddDefaultMap();

        ((int)map[ResultStatus.Ok].DefaultStatusCode).Should().Be(200);
        ((int)map[ResultStatus.NotFound].DefaultStatusCode).Should().Be(404);
        ((int)map[ResultStatus.Invalid].DefaultStatusCode).Should().Be(400);
        ((int)map[ResultStatus.Error].DefaultStatusCode).Should().Be(500);
    }

    [Fact]
    public void ResultStatusMap_Should_Override_For_Http_Method()
    {
        var map = new ResultStatusMap().AddDefaultMap();
        map.For(ResultStatus.Created, System.Net.HttpStatusCode.Created, opts =>
            opts.For("POST", System.Net.HttpStatusCode.Created));

        // Method override doğru şekilde ayarlandı mı?
        var options = map[ResultStatus.Created];
        ((int)options.GetStatusCode("POST")).Should().Be(201);
        map.ContainsKey(ResultStatus.Created).Should().BeTrue();
    }

    [Fact]
    public void ResultStatusMap_Remove_Should_Work()
    {
        var map = new ResultStatusMap().AddDefaultMap();
        map.Remove(ResultStatus.Unavailable);

        map.ContainsKey(ResultStatus.Unavailable).Should().BeFalse();
    }
}

/// <summary>
/// PagedResult için birim testleri.
/// </summary>
public class PagedResultTests
{
    [Fact]
    public void PagedInfo_Should_Calculate_TotalPages()
    {
        var info = new PagedInfo(1, 10, 95);

        info.TotalPages.Should().Be(10); // Math.Ceiling(95/10) = 10
    }

    [Fact]
    public void PagedInfo_Should_Handle_Zero_PageSize()
    {
        var info = new PagedInfo(1, 0, 50);

        info.TotalPages.Should().Be(0);
    }

    [Fact]
    public void PagedResult_Success_Should_Include_PagedInfo()
    {
        var data = new[] { "item1", "item2" };
        var pagedInfo = new PagedInfo(1, 10, 2);

        var result = PagedResult<string[]>.Success(data, pagedInfo);

        result.IsSuccess.Should().BeTrue();
        result.PagedInfo.Should().Be(pagedInfo);
        result.Value.Should().BeEquivalentTo(data);
    }
}
