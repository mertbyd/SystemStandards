namespace SystemStandards.Extensions;

using Microsoft.AspNetCore.Mvc;
using SystemStandards.Conventions;
using SystemStandards.Mapping;

/// <summary>
/// MvcOptions için Result pattern convention'larını ekleyen extension method'lar.
/// AddDefaultResultConvention — default map ile Swagger entegrasyonu.
/// AddResultConvention — custom map konfigürasyonu.
/// </summary>
public static class MvcOptionsExtensions
{
    /// <summary>
    /// Varsayılan ResultStatusMap ile Swagger ProducesResponseType convention'ını ekler.
    /// Kullanım: builder.Services.AddControllers(opts => opts.AddDefaultResultConvention(map));
    /// </summary>
    public static MvcOptions AddDefaultResultConvention(this MvcOptions options, ResultStatusMap map)
    {
        options.Conventions.Add(new ResultConvention(map));
        return options;
    }

    /// <summary>
    /// Custom ResultStatusMap konfigürasyonu ile convention ekler.
    /// Kullanım: builder.Services.AddControllers(opts => opts.AddResultConvention(map => map.AddDefaultMap()));
    /// </summary>
    public static MvcOptions AddResultConvention(this MvcOptions options, Action<ResultStatusMap> configure)
    {
        var map = new ResultStatusMap();
        configure(map);
        options.Conventions.Add(new ResultConvention(map));
        return options;
    }
}
