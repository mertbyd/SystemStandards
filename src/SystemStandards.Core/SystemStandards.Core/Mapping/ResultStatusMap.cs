namespace SystemStandards.Mapping;

using System.Net;
using SystemStandards.Results;

/// <summary>
/// ResultStatus'larını HTTP kodlarına ve response factory'lerine eşleyen fluent map.
/// Ardalis.Result pattern'ından uyarlanmıştır.
/// Kullanım: services.AddSystemStandardsCore(map => map.AddDefaultMap())
/// </summary>
public class ResultStatusMap
{
    private readonly Dictionary<ResultStatus, ResultStatusOptions> _map = new();

    /// <summary>
    /// Tüm standart ResultStatus → HTTP code eşlemelerini ekler.
    /// Ok→200, Created→201, NoContent→204, Invalid→400,
    /// Unauthorized→401, Forbidden→403, NotFound→404,
    /// Conflict→409, Error→500, CriticalError→500, Unavailable→503
    /// </summary>
    public ResultStatusMap AddDefaultMap()
    {
        For(ResultStatus.Ok, HttpStatusCode.OK);
        For(ResultStatus.Created, HttpStatusCode.Created);
        For(ResultStatus.NoContent, HttpStatusCode.NoContent);
        For(ResultStatus.Invalid, HttpStatusCode.BadRequest);
        For(ResultStatus.Unauthorized, HttpStatusCode.Unauthorized);
        For(ResultStatus.Forbidden, HttpStatusCode.Forbidden);
        For(ResultStatus.NotFound, HttpStatusCode.NotFound);
        For(ResultStatus.Conflict, HttpStatusCode.Conflict);
        For(ResultStatus.Error, HttpStatusCode.InternalServerError);
        For(ResultStatus.CriticalError, HttpStatusCode.InternalServerError);
        For(ResultStatus.Unavailable, HttpStatusCode.ServiceUnavailable);
        return this;
    }

    /// <summary>
    /// Belirli bir ResultStatus için HTTP kodu ve opsiyonel ayar ekler.
    /// Kullanım: map.For(ResultStatus.Error, HttpStatusCode.InternalServerError, opts => opts.For("POST", HttpStatusCode.UnprocessableEntity))
    /// </summary>
    public ResultStatusMap For(
        ResultStatus status,
        HttpStatusCode httpCode,
        Action<ResultStatusOptions>? configure = null)
    {
        var options = new ResultStatusOptions { DefaultStatusCode = httpCode };
        configure?.Invoke(options);
        _map[status] = options;
        return this;
    }

    /// <summary>
    /// Belirli bir ResultStatus'ı map'ten kaldırır.
    /// </summary>
    public ResultStatusMap Remove(ResultStatus status)
    {
        _map.Remove(status);
        return this;
    }

    /// <summary>
    /// Belirli bir ResultStatus için ayarları döner.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Status map'te yoksa fırlatılır.</exception>
    public ResultStatusOptions this[ResultStatus status] => _map[status];

    /// <summary>
    /// Map'te belirli bir status var mı?
    /// </summary>
    public bool ContainsKey(ResultStatus status) => _map.ContainsKey(status);

    /// <summary>
    /// Map'teki tüm status-options çiftlerini döner (Swagger convention için).
    /// </summary>
    public IReadOnlyDictionary<ResultStatus, ResultStatusOptions> All => _map;
}

/// <summary>
/// Tek bir ResultStatus için HTTP kodu ve response üretim ayarları.
/// HTTP method bazlı override ve custom response factory destekler.
/// </summary>
public class ResultStatusOptions
{
    /// <summary>Varsayılan HTTP status kodu</summary>
    public HttpStatusCode DefaultStatusCode { get; set; }

    /// <summary>
    /// HTTP method'a göre farklı status kodu.
    /// Örn: POST → 201, GET → 200
    /// </summary>
    public Dictionary<string, HttpStatusCode> MethodToStatusCodeMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Özel response body üreteci. Null ise varsayılan format kullanılır.
    /// </summary>
    public Func<IResult, object>? ResponseFactory { get; set; }

    /// <summary>
    /// Belirli HTTP method için status kodu atar (fluent).
    /// Kullanım: opts.For("POST", HttpStatusCode.Created)
    /// </summary>
    public ResultStatusOptions For(string httpMethod, HttpStatusCode code)
    {
        MethodToStatusCodeMap[httpMethod] = code;
        return this;
    }

    /// <summary>
    /// Custom response factory atar (fluent).
    /// Kullanım: opts.With(result => new { myField = result.GetValue() })
    /// </summary>
    public ResultStatusOptions With(Func<IResult, object> factory)
    {
        ResponseFactory = factory;
        return this;
    }

    /// <summary>
    /// HTTP method'a göre uygun status kodunu döner.
    /// Method-specific override yoksa DefaultStatusCode döner.
    /// </summary>
    public HttpStatusCode GetStatusCode(string httpMethod)
        => MethodToStatusCodeMap.TryGetValue(httpMethod, out var code) ? code : DefaultStatusCode;
}
