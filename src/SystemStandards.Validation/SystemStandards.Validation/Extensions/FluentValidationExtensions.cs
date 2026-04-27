namespace SystemStandards.Validation;

using FluentValidation;
using FluentValidation.Results;
using SystemStandards.Localization;

/// <summary>
/// FluentValidation kurallarına SystemStandards entegrasyonu sağlayan extension method'lar.
/// IValidationMessageProvider üzerinden localized mesajlar döner (hardcoded dictionary YOK).
/// DRY prensibi — tekrar eden validation kuralları bir kez yazılır.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// String property'nin boş olmadığını kontrol et + localized mesaj.
    /// Kullanım: RuleFor(x => x.Name).NotEmptyLocalized(provider, "REQUIRED")
    /// </summary>
    public static IRuleBuilderOptions<T, string?> NotEmptyLocalized<T>(
        this IRuleBuilder<T, string?> rule,
        IValidationMessageProvider messageProvider,
        string errorCode = "REQUIRED")
        => rule
            .NotEmpty()
            .WithErrorCode(errorCode)
            .WithMessage(_ => messageProvider.GetMessage(errorCode));

    /// <summary>
    /// String length validation + localized message.
    /// Kullanım: RuleFor(x => x.Code).MaximumLengthLocalized(provider, 50, "MAX_LENGTH")
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MaximumLengthLocalized<T>(
        this IRuleBuilder<T, string?> rule,
        IValidationMessageProvider messageProvider,
        int maxLength,
        string errorCode = "MAX_LENGTH")
        => rule
            .MaximumLength(maxLength)
            .WithErrorCode(errorCode)
            .WithMessage(_ => messageProvider.GetFormattedMessage(errorCode, maxLength));

    /// <summary>
    /// Minimum length validation + localized message.
    /// Kullanım: RuleFor(x => x.Name).MinimumLengthLocalized(provider, 3, "MIN_LENGTH")
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MinimumLengthLocalized<T>(
        this IRuleBuilder<T, string?> rule,
        IValidationMessageProvider messageProvider,
        int minLength,
        string errorCode = "MIN_LENGTH")
        => rule
            .MinimumLength(minLength)
            .WithErrorCode(errorCode)
            .WithMessage(_ => messageProvider.GetFormattedMessage(errorCode, minLength));

    /// <summary>
    /// Email validation + localized message.
    /// Kullanım: RuleFor(x => x.Email).EmailLocalized(provider)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> EmailLocalized<T>(
        this IRuleBuilder<T, string?> rule,
        IValidationMessageProvider messageProvider,
        string errorCode = "EMAIL_INVALID")
        => rule
            .EmailAddress()
            .WithErrorCode(errorCode)
            .WithMessage(_ => messageProvider.GetMessage(errorCode));

    /// <summary>
    /// Enum validation + localized message.
    /// Kullanım: RuleFor(x => x.Status).IsInEnumLocalized(provider, "INVALID_ENUM")
    /// </summary>
    public static IRuleBuilderOptions<T, TEnum> IsInEnumLocalized<T, TEnum>(
        this IRuleBuilder<T, TEnum> rule,
        IValidationMessageProvider messageProvider,
        string errorCode = "INVALID_ENUM")
        where TEnum : struct, Enum
        => rule
            .IsInEnum()
            .WithErrorCode(errorCode)
            .WithMessage(_ => messageProvider.GetMessage(errorCode));

    /// <summary>
    /// Unique validation — async custom validator.
    /// Kullanım: RuleFor(x => x.Code).UniqueAsync(provider, (code, ct) => repo.IsUniqueAsync(code, ct))
    /// </summary>
    public static IRuleBuilderOptionsConditions<T, string?> UniqueAsync<T>(
        this IRuleBuilder<T, string?> rule,
        IValidationMessageProvider messageProvider,
        Func<string?, CancellationToken, System.Threading.Tasks.Task<bool>> isUniqueAsync,
        string errorCode = "DUPLICATE",
        string fieldName = "Value")
        => rule.CustomAsync(async (value, context, ct) =>
        {
            if (value == null) return;

            var isUnique = await isUniqueAsync(value, ct);
            if (!isUnique)
            {
                context.AddFailure(new ValidationFailure(
                    propertyName: context.PropertyPath,
                    errorMessage: messageProvider.GetFormattedMessage(errorCode, fieldName),
                    attemptedValue: value)
                {
                    ErrorCode = errorCode
                });
            }
        });

    /// <summary>
    /// Comparison validation (greater than) + localized message.
    /// Kullanım: RuleFor(x => x.Price).GreaterThanLocalized(provider, 0, "COMPARISON_GREATER")
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> GreaterThanLocalized<T, TProperty>(
        this IRuleBuilder<T, TProperty> rule,
        IValidationMessageProvider messageProvider,
        TProperty valueToCompare,
        string errorCode = "COMPARISON_GREATER")
        where TProperty : IComparable<TProperty>, IComparable
        => rule
            .GreaterThan(valueToCompare)
            .WithErrorCode(errorCode)
            .WithMessage(_ => messageProvider.GetFormattedMessage(errorCode, valueToCompare?.ToString() ?? ""));

    /// <summary>
    /// Comparison validation (less than) + localized message.
    /// Kullanım: RuleFor(x => x.Quantity).LessThanLocalized(provider, 1000, "COMPARISON_LESS")
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> LessThanLocalized<T, TProperty>(
        this IRuleBuilder<T, TProperty> rule,
        IValidationMessageProvider messageProvider,
        TProperty valueToCompare,
        string errorCode = "COMPARISON_LESS")
        where TProperty : IComparable<TProperty>, IComparable
        => rule
            .LessThan(valueToCompare)
            .WithErrorCode(errorCode)
            .WithMessage(_ => messageProvider.GetFormattedMessage(errorCode, valueToCompare?.ToString() ?? ""));

    /// <summary>
    /// Range validation + localized message.
    /// Kullanım: RuleFor(x => x.Age).InclusiveBetweenLocalized(provider, 18, 120, "RANGE")
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> InclusiveBetweenLocalized<T, TProperty>(
        this IRuleBuilder<T, TProperty> rule,
        IValidationMessageProvider messageProvider,
        TProperty from,
        TProperty to,
        string errorCode = "RANGE")
        where TProperty : IComparable<TProperty>, IComparable
        => rule
            .InclusiveBetween(from, to)
            .WithErrorCode(errorCode)
            .WithMessage(_ => messageProvider.GetFormattedMessage(errorCode, new object?[] { from?.ToString() ?? "", to?.ToString() ?? "" }));

    // ==================== FLUENT VALIDATION → VALIDATION ERRORS ====================

    /// <summary>
    /// FluentValidation ValidationResult'ı SystemStandards ValidationError listesine çevirir.
    /// Kullanım: validator.Validate(dto).ToValidationErrors()
    /// </summary>
    public static IReadOnlyList<SystemStandards.Results.ValidationError> ToValidationErrors(
        this FluentValidation.Results.ValidationResult result)
        => result.Errors
            .Select(f => new SystemStandards.Results.ValidationError(
                Identifier: f.PropertyName,
                ErrorMessage: f.ErrorMessage,
                ErrorCode: f.ErrorCode ?? "VALIDATION_ERROR",
                Severity: f.Severity switch
                {
                    Severity.Error => SystemStandards.Results.ValidationSeverity.Error,
                    Severity.Warning => SystemStandards.Results.ValidationSeverity.Warning,
                    Severity.Info => SystemStandards.Results.ValidationSeverity.Info,
                    _ => SystemStandards.Results.ValidationSeverity.Error
                }))
            .ToList();
}
