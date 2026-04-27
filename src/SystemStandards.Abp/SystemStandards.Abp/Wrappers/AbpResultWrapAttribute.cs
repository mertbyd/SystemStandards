namespace SystemStandards.Abp.Wrappers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SystemStandards.Extensions;
using SystemStandards.Results;

/// <summary>
/// ABP projelerinde opt-in Result wrap attribute.
/// ABP'nin kaldırılan WrapResultFilter'ının yerine geçer.
/// Kullanım: [AbpResultWrap] attribute ile action'a uygula.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AbpResultWrapAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Action çalıştıktan sonra IResult → ActionResult dönüşümü yapar (ABP aware).
    /// </summary>
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is not ObjectResult { Value: IResult result })
            return;

        if (context.Controller is not ControllerBase controller)
            return;

        context.Result = controller.ToActionResult(result);
    }
}
