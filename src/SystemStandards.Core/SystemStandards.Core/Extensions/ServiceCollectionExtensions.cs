namespace SystemStandards.Extensions;

using Microsoft.Extensions.DependencyInjection;
using SystemStandards.Mapping;
using SystemStandards.Options;

/// <summary>
/// IServiceCollection'a SystemStandards Core servislerini register eden extension method'lar.
/// Fluent ResultStatusMap API ile konfigürasyon yapılır.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// SystemStandards Core servislerini DI container'a ekler.
    /// Varsayılan ResultStatusMap kullanılır (AddDefaultMap çağrılır).
    /// Kullanım: builder.Services.AddSystemStandardsCore();
    /// </summary>
    public static IServiceCollection AddSystemStandardsCore(
        this IServiceCollection services)
        => services.AddSystemStandardsCore(map => map.AddDefaultMap());

    /// <summary>
    /// SystemStandards Core servislerini fluent konfigürasyon ile ekler.
    /// Kullanım:
    /// <code>
    /// builder.Services.AddSystemStandardsCore(map => {
    ///     map.AddDefaultMap()
    ///        .For(ResultStatus.Conflict, HttpStatusCode.Conflict);
    /// });
    /// </code>
    /// </summary>
    public static IServiceCollection AddSystemStandardsCore(
        this IServiceCollection services,
        Action<ResultStatusMap> configureMap)
    {
        // 1. ResultStatusMap singleton oluştur ve fluent konfigürasyonu uygula
        var map = new ResultStatusMap();
        configureMap(map);
        services.AddSingleton(map);

        // 2. IResultStatusMapper — singleton (map'e bağlı)
        services.AddSingleton<IResultStatusMapper, ResultStatusMapper>();

        // 3. ExceptionMappingOptions — varsayılan değerlerle
        services.AddOptions<ExceptionMappingOptions>();

        // 4. IExceptionToResultStatusResolver — singleton, type-safe hierarchy walk
        services.AddSingleton<IExceptionToResultStatusResolver, DefaultExceptionToResultStatusResolver>();

        return services;
    }
}
