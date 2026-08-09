namespace SystemStandards.Abp.Authorization.Context;

/// <summary>
/// Çalışan isteğin seçili bağlamını doğrulanmış token'dan typed olarak okuyan port.
/// Eksik veya bozuk claim'de ambient bir değere düşülmez; fail-closed sonuç döner.
/// </summary>
public interface ISelectedContextAccessor
{
    /// <summary>Çalışan isteğin bağlamını okur.</summary>
    /// <returns>Okunan bağlamı ya da ret gerekçesini taşıyan sonuç.</returns>
    SelectedContextResolution Resolve();
}
