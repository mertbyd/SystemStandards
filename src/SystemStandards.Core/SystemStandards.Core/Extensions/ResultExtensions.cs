namespace SystemStandards.Extensions;

using SystemStandards.Results;

/// <summary>
/// Result monadik operasyonlar — Map, Bind, Filter, MapAsync.
/// Ardalis pattern'ından uyarlanmıştır.
/// Başarısız Result'larda mapper/binder çağrılmaz — hata olduğu gibi iletilir.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Başarılı Result'ın değerini dönüştürür.
    /// Hata durumunda mapper çağrılmaz, hata iletilir.
    /// Kullanım: result.Map(dto => new ViewModel(dto))
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (!result.IsSuccess || result.Value is null)
            return PropagateError<TIn, TOut>(result);

        var mapped = mapper(result.Value);
        var output = Result<TOut>.Success(mapped, result.SuccessMessage);
        return output
            .WithCorrelationId(result.CorrelationId)
            .WithLocation(result.Location);
    }

    /// <summary>
    /// Async Map — başarılı Result'ın değerini asenkron olarak dönüştürür.
    /// Kullanım: await result.MapAsync(async dto => await enrichAsync(dto))
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (!result.IsSuccess || result.Value is null)
            return PropagateError<TIn, TOut>(result);

        var mapped = await mapper(result.Value);
        var output = Result<TOut>.Success(mapped, result.SuccessMessage);
        return output
            .WithCorrelationId(result.CorrelationId)
            .WithLocation(result.Location);
    }

    /// <summary>
    /// Başarılı Result'ın değerini filtreler.
    /// Predicate false dönerse NotFound döner.
    /// Kullanım: result.Filter(dto => dto.IsActive, "Kayıt aktif değil")
    /// </summary>
    public static Result<T> Filter<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        string failureMessage = "Filtre koşulu sağlanamadı")
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (!result.IsSuccess || result.Value is null)
            return result;

        if (!predicate(result.Value))
            return Result<T>.NotFound(failureMessage);

        return result;
    }

    /// <summary>
    /// Başarılı Result'ın değerini başka bir Result döndüren fonksiyona bağlar.
    /// Monadik Bind (flatMap) — zincirleme için kullanılır.
    /// Kullanım: result.Bind(dto => _service.ProcessAsync(dto))
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (!result.IsSuccess || result.Value is null)
            return PropagateError<TIn, TOut>(result);

        return binder(result.Value);
    }

    /// <summary>
    /// Async Bind — başarılı Result'ı asenkron binder'a bağlar.
    /// Kullanım: await result.BindAsync(dto => _service.ProcessAsync(dto))
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (!result.IsSuccess || result.Value is null)
            return PropagateError<TIn, TOut>(result);

        return await binder(result.Value);
    }

    // ==================== PRIVATE HELPERS ====================

    /// <summary>
    /// Başarısız Result'ın hata bilgilerini yeni tipte propagate eder.
    /// Factory metodları üzerinden yapar (protected set erişim kısıtı nedeniyle).
    /// </summary>
    private static Result<TOut> PropagateError<TIn, TOut>(Result<TIn> source)
    {
        Result<TOut> target = source.Status switch
        {
            ResultStatus.NotFound => Result<TOut>.NotFound(source.Errors.FirstOrDefault()),
            ResultStatus.Unauthorized => Result<TOut>.Unauthorized(source.Errors.FirstOrDefault()),
            ResultStatus.Forbidden => Result<TOut>.Forbidden(source.Errors.FirstOrDefault()),
            ResultStatus.Conflict => Result<TOut>.Conflict(source.Errors.FirstOrDefault()),
            ResultStatus.CriticalError => Result<TOut>.CriticalError(source.Errors.FirstOrDefault()),
            ResultStatus.Unavailable => Result<TOut>.Unavailable(source.Errors.FirstOrDefault()),
            ResultStatus.Invalid => Result<TOut>.Invalid(source.ValidationErrors.ToArray()),
            ResultStatus.Error => source.Errors.Count > 0
                ? Result<TOut>.Error(source.Errors.ToArray())
                : Result<TOut>.Error("İşlem başarısız"),
            _ => Result<TOut>.Error("Bilinmeyen hata")
        };

        return target
            .WithCorrelationId(source.CorrelationId)
            .WithLocation(source.Location);
    }
}
