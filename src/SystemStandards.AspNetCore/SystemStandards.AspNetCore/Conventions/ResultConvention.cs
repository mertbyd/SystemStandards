namespace SystemStandards.Conventions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using SystemStandards.Filters;
using SystemStandards.Mapping;

/// <summary>
/// TranslateResultToActionResultAttribute içeren action'lara otomatik olarak
/// [ProducesResponseType] attribute'ları ekleyen IApplicationModelConvention.
/// Swagger/OpenAPI UI'da tüm olası response code'ları görünür.
/// </summary>
public class ResultConvention : IApplicationModelConvention
{
    private readonly ResultStatusMap _map;

    /// <summary>
    /// ResultConvention — ResultStatusMap DI'dan inject edilir.
    /// </summary>
    public ResultConvention(ResultStatusMap map)
    {
        _map = map;
    }

    /// <summary>
    /// Uygulama modelindeki tüm controller ve action'ları dolaşarak
    /// TranslateResultToActionResultAttribute olan action'lara
    /// ProducesResponseType attribute'ları ekler.
    /// </summary>
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            // Controller seviyesinde attribute var mı?
            var controllerHasAttr = controller.Filters
                .Any(f => f is TranslateResultToActionResultAttribute);

            foreach (var action in controller.Actions)
            {
                var actionHasAttr = action.Filters
                    .Any(f => f is TranslateResultToActionResultAttribute);

                if (!controllerHasAttr && !actionHasAttr)
                    continue;

                // Mevcut response code'larını topla
                var existingCodes = action.Filters
                    .OfType<ProducesResponseTypeAttribute>()
                    .Select(a => a.StatusCode)
                    .ToHashSet();

                // Map'teki her status için ProducesResponseType ekle
                foreach (var (_, options) in _map.All)
                {
                    var code = (int)options.DefaultStatusCode;
                    if (!existingCodes.Contains(code))
                    {
                        action.Filters.Add(new ProducesResponseTypeAttribute(code));
                        existingCodes.Add(code);
                    }
                }
            }
        }
    }
}
