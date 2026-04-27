namespace SystemStandards.Mapping;

using System.Net;
using SystemStandards.Results;

/// <summary>
/// ResultStatus enum'ını HTTP status code'larına eşleyen servis contract.
/// Tüm mapping ResultStatusMap üzerinden çalışır — hardcoding yok.
/// </summary>
public interface IResultStatusMapper
{
    /// <summary>
    /// ResultStatus → HTTP status code dönüşümü (GET için varsayılan)
    /// Örn: ResultStatus.NotFound → 404
    /// </summary>
    int MapToHttpStatusCode(ResultStatus status);

    /// <summary>
    /// ResultStatus → HTTP status code (HTTP method'a göre override)
    /// Örn: ResultStatus.Created + POST → 201, GET → 200
    /// </summary>
    int MapToHttpStatusCode(ResultStatus status, string httpMethod);

    /// <summary>
    /// HTTP status code'u ResultStatus'e ters eşleme (opsiyonel)
    /// Örn: 404 → ResultStatus.NotFound
    /// </summary>
    ResultStatus? MapFromHttpStatusCode(int httpStatusCode);
}
