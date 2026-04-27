
namespace SystemStandards.Results;

/// <summary>
/// Operasyon sonuçlarını temsil eden base class (generic olmayan).
/// Veri döndürmeyen işlemler (Delete, Update vb.) için kullanılır.
/// Örn: await _service.DeleteAsync(id) -> Result (bool değil)
/// </summary>
public class Result : IResult
{
    /// <summary>İşlem başarılı mı?</summary>
    public bool IsSuccess => Status == ResultStatus.Ok
        || Status == ResultStatus.Created
        || Status == ResultStatus.NoContent;

    /// <summary>Operasyon durumu</summary>
    public ResultStatus Status { get; protected set; }

    /// <summary>Hata mesajları (başarısızlıkta doldurulur)</summary>
    public IReadOnlyList<string> Errors { get; protected set; } = new List<string>();

    /// <summary>Doğrulama hataları (Invalid status'ta)</summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; protected set; } = new List<ValidationError>();

    /// <summary>Başarı mesajı</summary>
    public string? SuccessMessage { get; protected set; }

    /// <summary>Korelasyon ID (logging için)</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Yeni kaynağın konumu (Created status'ta)</summary>
    public string? Location { get; set; }

    /// <summary>Veri yok (generic olmayan). Subclass'lar override edebilir.</summary>
    public virtual object? GetValue() => null;

    // ==================== PROTECTED HELPERS (DRY — Result<T> kullanır) ====================

    /// <summary>
    /// Belirli status + hatalar ile hata sonucu oluşturur. Result&lt;T&gt; factory'leri için DRY helper.
    /// </summary>
    protected static TResult CreateError<TResult>(ResultStatus status, params string[] errors)
        where TResult : Result, new()
        => new() { Status = status, Errors = errors.ToList() };

    /// <summary>
    /// Invalid status ile doğrulama hatalı sonuç oluşturur. Result&lt;T&gt; factory'leri için DRY helper.
    /// </summary>
    protected static TResult CreateInvalid<TResult>(params ValidationError[] errors)
        where TResult : Result, new()
        => new() { Status = ResultStatus.Invalid, ValidationErrors = errors.ToList() };

    // ==================== STATIC FACTORY METHODS ====================

    /// <summary>
    /// Başarılı sonuç döner (HTTP 200 OK)
    /// Kullanım: return Result.Success();
    /// </summary>
    public static Result Success(string? message = null)
        => new()
        {
            Status = ResultStatus.Ok,
            SuccessMessage = message
        };

    /// <summary>
    /// Kaynak oluşturma başarısı (HTTP 201 Created)
    /// Location header'ında yeni kaynağın URL'i vardır.
    /// Kullanım: return Result.Created("/api/products/123", "Ürün oluşturuldu");
    /// </summary>
    public static Result Created(string location, string? message = null)
        => new()
        {
            Status = ResultStatus.Created,
            Location = location,
            SuccessMessage = message ?? "Kaynak başarıyla oluşturuldu"
        };

    /// <summary>
    /// İçeriksiz başarılı sonuç (HTTP 204 No Content)
    /// Kullanım: return Result.NoContent();
    /// </summary>
    public static Result NoContent()
        => new()
        {
            Status = ResultStatus.NoContent,
            SuccessMessage = "İşlem başarılı, veri döndürülmedi"
        };

    /// <summary>
    /// Genel hata döner (HTTP 500 Internal Server Error)
    /// Kullanım: return Result.Error("Database bağlantısı başarısız");
    /// </summary>
    public static Result Error(params string[] errors)
        => new()
        {
            Status = ResultStatus.Error,
            Errors = errors.ToList()
        };

    /// <summary>
    /// ErrorList ile hata döner (CorrelationId dahil birden fazla hata).
    /// Kullanım: return Result.Error(new ErrorList(errors, correlationId));
    /// </summary>
    public static Result Error(Errors.ErrorList errorList)
        => new()
        {
            Status = ResultStatus.Error,
            Errors = errorList.ToList(),
            CorrelationId = errorList.CorrelationId
        };

    /// <summary>
    /// Doğrulama hatası döner (HTTP 400 Bad Request)
    /// Kullanım: return Result.Invalid(
    ///     new("Name", "Ad boş olamaz", "REQUIRED"),
    ///     new("Email", "Email invalid", "INVALID_FORMAT")
    /// );
    /// </summary>
    public static Result Invalid(params ValidationError[] errors)
        => new()
        {
            Status = ResultStatus.Invalid,
            ValidationErrors = errors.ToList()
        };

    /// <summary>
    /// Kaynak bulunamadı (HTTP 404 Not Found)
    /// Kullanım: return Result.NotFound();
    /// </summary>
    public static Result NotFound(string? message = null)
        => new()
        {
            Status = ResultStatus.NotFound,
            Errors = new[] { message ?? "Kaynak bulunamadı" }.ToList()
        };

    /// <summary>
    /// Erişim yasaklı (HTTP 403 Forbidden)
    /// Kullanım: return Result.Forbidden("Bu kaynağı silmeye yetkiniz yok");
    /// </summary>
    public static Result Forbidden(string? message = null)
        => new()
        {
            Status = ResultStatus.Forbidden,
            Errors = new[] { message ?? "Erişim yasaklı" }.ToList()
        };

    /// <summary>
    /// Yetkisiz erişim (HTTP 401 Unauthorized)
    /// Kullanım: return Result.Unauthorized();
    /// </summary>
    public static Result Unauthorized(string? message = null)
        => new()
        {
            Status = ResultStatus.Unauthorized,
            Errors = new[] { message ?? "Lütfen oturum açın" }.ToList()
        };

    /// <summary>
    /// Kaynak çakışması (HTTP 409 Conflict)
    /// Örn: Duplicate entry, concurrent modification
    /// Kullanım: return Result.Conflict("Ürün kodu zaten mevcut");
    /// </summary>
    public static Result Conflict(string? message = null)
        => new()
        {
            Status = ResultStatus.Conflict,
            Errors = new[] { message ?? "Kaynak çakışması" }.ToList()
        };

    /// <summary>
    /// Kritik hata (HTTP 500, immediate action gerekli)
    /// Kullanım: return Result.CriticalError("Payment gateway down");
    /// </summary>
    public static Result CriticalError(string? message = null)
        => new()
        {
            Status = ResultStatus.CriticalError,
            Errors = new[] { message ?? "Kritik hata oluştu" }.ToList()
        };

    /// <summary>
    /// Hizmet kullanılamaz (HTTP 503 Service Unavailable)
    /// Kullanım: return Result.Unavailable("Veritabanı bakımda");
    /// </summary>
    public static Result Unavailable(string? message = null)
        => new()
        {
            Status = ResultStatus.Unavailable,
            Errors = new[] { message ?? "Hizmet geçici olarak kullanılamaz" }.ToList()
        };

    // ==================== FLUENT METHODS ====================

    /// <summary>
    /// Sonuca Correlation ID ekler ve kendisini döner.
    /// </summary>
    public Result WithCorrelationId(string? correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    /// <summary>
    /// Sonuca Location ekler ve kendisini döner.
    /// </summary>
    public Result WithLocation(string? location)
    {
        Location = location;
        return this;
    }
}
