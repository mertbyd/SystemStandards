using FluentAssertions;
using NSubstitute;
using SystemStandards.Localization;
using SystemStandards.Validation;
using Xunit;

namespace SystemStandards.Tests;

/// <summary>
/// FluentValidation extension'ları ve ValidationError dönüşümü için birim testleri.
/// </summary>
public class FluentValidationExtensionsTests
{
    [Fact]
    public void ToValidationErrors_Should_Convert_Failures()
    {
        var validationResult = new FluentValidation.Results.ValidationResult(
        [
            new FluentValidation.Results.ValidationFailure("Name", "Ad boş olamaz")
            {
                ErrorCode = "REQUIRED"
            },
            new FluentValidation.Results.ValidationFailure("Email", "Email hatalı")
            {
                ErrorCode = "EMAIL_INVALID"
            }
        ]);

        var errors = validationResult.ToValidationErrors();

        errors.Should().HaveCount(2);
        errors[0].Identifier.Should().Be("Name");
        errors[0].ErrorCode.Should().Be("REQUIRED");
        errors[1].Identifier.Should().Be("Email");
    }

    [Fact]
    public void IValidationMessageProvider_Should_Return_Message_When_Mocked()
    {
        var provider = Substitute.For<IValidationMessageProvider>();
        provider.GetMessage("REQUIRED").Returns("Bu alan zorunludur");

        var message = provider.GetMessage("REQUIRED");

        message.Should().Be("Bu alan zorunludur");
    }

    [Fact]
    public void IValidationMessageProvider_Should_Return_Formatted_Message()
    {
        var provider = Substitute.For<IValidationMessageProvider>();
        provider.GetFormattedMessage("MAX_LENGTH", 50).Returns("Alan maksimum 50 karakter olmalıdır");

        var message = provider.GetFormattedMessage("MAX_LENGTH", 50);

        message.Should().Be("Alan maksimum 50 karakter olmalıdır");
    }
}
