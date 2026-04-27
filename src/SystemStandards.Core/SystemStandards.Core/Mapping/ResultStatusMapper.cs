namespace SystemStandards.Mapping;

using System.Net;
using Microsoft.Extensions.Logging;
using SystemStandards.Results;

/// <summary>
/// ResultStatus'ı HTTP kodlarına eşleyen varsayılan implementation.
/// ResultStatusMap (Dictionary tabanlı) kullanır — O(1) lookup, B9 fix.
/// </summary>
public class ResultStatusMapper : IResultStatusMapper
{
    private readonly ResultStatusMap _map;
    private readonly ILogger<ResultStatusMapper> _logger;

    /// <summary>
    /// Constructor — ResultStatusMap ve Logger DI ile inject edilir.
    /// </summary>
    public ResultStatusMapper(ResultStatusMap map, ILogger<ResultStatusMapper> logger)
    {
        _map = map;
        _logger = logger;
    }

    /// <summary>
    /// ResultStatus → HTTP status code (GET için varsayılan).
    /// </summary>
    public int MapToHttpStatusCode(ResultStatus status)
        => MapToHttpStatusCode(status, "GET");

    /// <summary>
    /// ResultStatus → HTTP status code (HTTP method bazlı).
    /// Method-specific override yoksa DefaultStatusCode döner.
    /// </summary>
    public int MapToHttpStatusCode(ResultStatus status, string httpMethod)
    {
        if (!_map.ContainsKey(status))
        {
            _logger.LogWarning(
                "HTTP status code mapping bulunamadı — ResultStatus: {Status}. Varsayılan: 500",
                status);
            return 500;
        }

        var options = _map[status];
        var code = options.GetStatusCode(httpMethod);

        _logger.LogDebug(
            "ResultStatus {Status} → HTTP {HttpCode} (method: {Method})",
            status, (int)code, httpMethod);

        return (int)code;
    }

    /// <summary>
    /// HTTP status code → ResultStatus (ters eşleme).
    /// Örn: 404 → ResultStatus.NotFound
    /// </summary>
    public ResultStatus? MapFromHttpStatusCode(int httpStatusCode)
    {
        foreach (var (status, options) in _map.All)
        {
            if ((int)options.DefaultStatusCode == httpStatusCode)
                return status;
        }

        _logger.LogWarning("HTTP {HttpCode} için ResultStatus bulunamadı", httpStatusCode);
        return null;
    }
}
