namespace SystemStandards.Extensions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SystemStandards.Mapping;
using SystemStandards.Results;
using IResult = SystemStandards.Results.IResult;

/// <summary>
/// ControllerBase için IResult → ActionResult çevirisi yapan extension method'lar.
/// ResultStatusMap'ten HTTP kodu alır, Location header'ını doğru ekler.
/// </summary>
public static class ActionResultExtensions
{
    /// <summary>
    /// IResult'ı uygun ActionResult'a çevirir.
    /// ResultStatusMap'ten HTTP kodu okunur, HTTP method'a göre override desteklenir.
    /// Created status'ta Location header yazılır (RFC 7231 — B10 fix).
    /// </summary>
    public static ActionResult ToActionResult(this ControllerBase controller, IResult result)
    {
        var map = controller.HttpContext.RequestServices.GetRequiredService<ResultStatusMap>();
        var httpMethod = controller.HttpContext.Request.Method;

        if (!map.ContainsKey(result.Status))
        {
            return new ObjectResult(BuildErrorBody(result, 500, "UNKNOWN_STATUS"))
            {
                StatusCode = 500
            };
        }

        var options = map[result.Status];
        var statusCode = (int)options.GetStatusCode(httpMethod);

        // B10 fix: Created status'ta Location header ekle (RFC 7231)
        if (result.Status == ResultStatus.Created && !string.IsNullOrWhiteSpace(result.Location))
        {
            controller.HttpContext.Response.Headers.Location = result.Location;
        }

        // Custom response factory varsa kullan
        if (options.ResponseFactory is not null)
        {
            var customBody = options.ResponseFactory(result);
            return new ObjectResult(customBody) { StatusCode = statusCode };
        }

        // Varsayılan response body
        var responseBody = BuildDefaultBody(result, statusCode);
        return new ObjectResult(responseBody) { StatusCode = statusCode };
    }

    private static object BuildDefaultBody(IResult result, int statusCode)
    {
        if (result.IsSuccess)
        {
            return new
            {
                success = true,
                statusCode,
                data = result.GetValue(),
                message = result.SuccessMessage,
                correlationId = result.CorrelationId,
                location = result.Location
            };
        }

        return BuildErrorBody(result, statusCode, null);
    }

    private static object BuildErrorBody(IResult result, int statusCode, string? defaultCode)
    {
        if (result.Status == ResultStatus.Invalid)
        {
            return new
            {
                success = false,
                statusCode,
                error = new
                {
                    code = "VALIDATION_FAILED",
                    message = "Doğrulama hatası",
                    validationErrors = result.ValidationErrors?.Select(ve => new
                    {
                        message = ve.ErrorMessage,
                        members = new[] { ve.Identifier },
                        errorCode = ve.ErrorCode,
                        severity = ve.Severity.ToString()
                    }).ToList()
                },
                correlationId = result.CorrelationId
            };
        }

        return new
        {
            success = false,
            statusCode,
            error = new
            {
                code = defaultCode ?? result.Status.ToString().ToUpperInvariant(),
                message = result.Errors?.FirstOrDefault() ?? "İşlem başarısız",
                details = result.Errors?.Count > 1
                    ? string.Join(Environment.NewLine, result.Errors.Skip(1))
                    : null
            },
            correlationId = result.CorrelationId
        };
    }
}
