namespace SystemStandards.Filters;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SystemStandards.Extensions;
using SystemStandards.Results;

/// <summary>
/// Action'ın döndürdüğü IResult'ı HTTP ActionResult'a çeviren action-level filter attribute.
/// Ardalis.Result pattern'ından uyarlanmıştır.
/// Kullanım: [TranslateResultToActionResult] attribute ile action'a uygula.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TranslateResultToActionResultAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Action çalıştıktan sonra IResult → ActionResult dönüşümü yapar.
    /// </summary>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        // Sadece ObjectResult içinde IResult varsa işle
        if (context.Result is not ObjectResult { Value: IResult result })
            return;

        // Controller context gerekli (Location header için)
        if (context.Controller is not ControllerBase controller)
            return;

        context.Result = controller.ToActionResult(result);
    }
}
