using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NSubstitute;
using SystemStandards.Abp.ExceptionHandling;
using SystemStandards.Mapping;
using SystemStandards.Results;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.ExceptionHandling.Localization;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Validation;
using Xunit;

namespace SystemStandards.Abp.Tests;

/// <summary>ResultBasedExceptionToErrorInfoConverter için birim testleri.</summary>
public class ResultBasedExceptionToErrorInfoConverterTests
{
    /// <summary>ABP boş bıraktığında ResultStatus kodunu korur.</summary>
    [Fact]
    public void Convert_Should_Set_ResultStatus_Code_When_Abp_Leaves_Code_Empty()
    {
        var statusResolver = Substitute.For<IExceptionToResultStatusResolver>();
        statusResolver.Resolve(Arg.Any<Exception>()).Returns(ResultStatus.Invalid);
        var converter = new ResultBasedExceptionToErrorInfoConverter(
            CreateDefaultConverter(),
            statusResolver);
        var exception = new AbpValidationException("Validation failed")
        {
            ValidationErrors =
            {
                new ValidationResult(
                    "DatabaseChecker.ComparisonDefinition:Validation:NameRequired",
                    new[] { "name" })
            }
        };

        var errorInfo = converter.Convert(exception, includeSensitiveDetails: false);

        errorInfo.Code.Should().Be("INVALID");
        errorInfo.ValidationErrors.Should().ContainSingle();
        errorInfo.ValidationErrors![0].Message.Should().Be("DatabaseChecker.ComparisonDefinition:Validation:NameRequired");
        errorInfo.ValidationErrors[0].Members.Should().ContainSingle("name");
    }

    // ABP'nin gerçek default converter bağımlılıklarını test container'ında kurar.
    private static DefaultExceptionToErrorInfoConverter CreateDefaultConverter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        var serviceProvider = services.BuildServiceProvider();

        return new DefaultExceptionToErrorInfoConverter(
            Microsoft.Extensions.Options.Options.Create(new AbpExceptionHandlingOptions()),
            Microsoft.Extensions.Options.Options.Create(new AbpExceptionLocalizationOptions()),
            serviceProvider.GetRequiredService<IStringLocalizerFactory>(),
            serviceProvider.GetRequiredService<IStringLocalizer<AbpExceptionHandlingResource>>(),
            serviceProvider);
    }
}
