namespace SystemStandards.Results;

/// <summary>
/// Generic operasyon sonucu — veri döndüren işlemler için.
/// Ardalis.Result pattern'ından uyarlanmıştır, hardcoding yok.
/// Örn: Task&lt;Result&lt;ProductDto&gt;&gt; GetAsync(id)
/// </summary>
public class Result<T> : Result
{
    /// <summary>Döndürülen veri (başarı durumunda)</summary>
    public T? Value { get; protected set; }

    /// <summary>Veriyi object olarak döner (IResult uyumluluğu için)</summary>
    public override object? GetValue() => Value;

    // ==================== STATIC FACTORY METHODS ====================

    /// <summary>
    /// Başarılı sonuç (veri ile)
    /// Kullanım: return Result&lt;ProductDto&gt;.Success(dto);
    /// </summary>
    public static Result<T> Success(T data, string? message = null)
        => new()
        {
            Status = ResultStatus.Ok,
            Value = data,
            SuccessMessage = message
        };

    /// <summary>
    /// Kaynak oluşturma başarısı (veri + location ile)
    /// Kullanım: return Result&lt;ProductDto&gt;.Created(newDto, "/api/products/123");
    /// </summary>
    public static Result<T> Created(T data, string location, string? message = null)
        => new()
        {
            Status = ResultStatus.Created,
            Value = data,
            Location = location,
            SuccessMessage = message ?? "Kaynak başarıyla oluşturuldu"
        };

    /// <summary>
    /// İçeriksiz başarılı sonuç (veri olmadan)
    /// Kullanım: return Result&lt;ProductDto&gt;.NoContent();
    /// </summary>
    public static new Result<T> NoContent()
        => new()
        {
            Status = ResultStatus.NoContent,
            SuccessMessage = "İşlem başarılı, veri döndürülmedi"
        };

    /// <summary>
    /// Genel hata
    /// Kullanım: return Result&lt;ProductDto&gt;.Error("Operation failed");
    /// </summary>
    public static new Result<T> Error(params string[] errors)
        => CreateError<Result<T>>(ResultStatus.Error, errors);

    /// <summary>
    /// ErrorList ile hata döner (CorrelationId dahil birden fazla hata).
    /// </summary>
    public static new Result<T> Error(Errors.ErrorList errorList)
        => new()
        {
            Status = ResultStatus.Error,
            Errors = errorList.ToList(),
            CorrelationId = errorList.CorrelationId
        };

    /// <summary>
    /// Doğrulama hatası
    /// Kullanım: return Result&lt;ProductDto&gt;.Invalid(validationErrors);
    /// </summary>
    public static new Result<T> Invalid(params ValidationError[] errors)
        => CreateInvalid<Result<T>>(errors);

    /// <summary>
    /// Kaynak bulunamadı
    /// Kullanım: return Result&lt;ProductDto&gt;.NotFound();
    /// </summary>
    public static new Result<T> NotFound(string? message = null)
        => CreateError<Result<T>>(ResultStatus.NotFound, message ?? "Kaynak bulunamadı");

    /// <summary>
    /// Erişim yasaklı
    /// Kullanım: return Result&lt;ProductDto&gt;.Forbidden();
    /// </summary>
    public static new Result<T> Forbidden(string? message = null)
        => CreateError<Result<T>>(ResultStatus.Forbidden, message ?? "Erişim yasaklı");

    /// <summary>
    /// Yetkisiz erişim
    /// Kullanım: return Result&lt;ProductDto&gt;.Unauthorized();
    /// </summary>
    public static new Result<T> Unauthorized(string? message = null)
        => CreateError<Result<T>>(ResultStatus.Unauthorized, message ?? "Lütfen oturum açın");

    /// <summary>
    /// Kaynak çakışması
    /// Kullanım: return Result&lt;ProductDto&gt;.Conflict("Code already exists");
    /// </summary>
    public static new Result<T> Conflict(string? message = null)
        => CreateError<Result<T>>(ResultStatus.Conflict, message ?? "Kaynak çakışması");

    /// <summary>
    /// Kritik hata
    /// Kullanım: return Result&lt;ProductDto&gt;.CriticalError();
    /// </summary>
    public static new Result<T> CriticalError(string? message = null)
        => CreateError<Result<T>>(ResultStatus.CriticalError, message ?? "Kritik hata oluştu");

    /// <summary>
    /// Hizmet kullanılamaz
    /// Kullanım: return Result&lt;ProductDto&gt;.Unavailable();
    /// </summary>
    public static new Result<T> Unavailable(string? message = null)
        => CreateError<Result<T>>(ResultStatus.Unavailable, message ?? "Hizmet geçici olarak kullanılamaz");

    // ==================== IMPLICIT CONVERSIONS ====================

    /// <summary>
    /// T → Result&lt;T&gt; implicit dönüşümü — null ise NotFound döner (RFC 7231 anlamlı).
    /// Kullanım: Result&lt;ProductDto&gt; result = productDto;
    /// </summary>
    public static implicit operator Result<T>(T? value)
        => value is null ? NotFound() : Success(value);

    // ==================== FLUENT METHODS ====================

    /// <summary>
    /// Sonuca Correlation ID ekler ve kendisini döner.
    /// </summary>
    public new Result<T> WithCorrelationId(string? correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    /// <summary>
    /// Sonuca Location ekler ve kendisini döner.
    /// </summary>
    public new Result<T> WithLocation(string? location)
    {
        Location = location;
        return this;
    }
}
