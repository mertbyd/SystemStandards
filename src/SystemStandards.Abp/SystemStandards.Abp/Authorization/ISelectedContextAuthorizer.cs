namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Operasyon adını karara çeviren port.
/// Kararın sahibi authorization server'dır; bu port yalnız soruyu taşır.
/// </summary>
public interface ISelectedContextAuthorizer
{
    /// <summary>Çalışan actor'ün seçili bağlamda sorulan operasyonu yapıp yapamayacağını değerlendirir.</summary>
    /// <param name="operationName">İzni sorulan operasyonun kararlı adı.</param>
    /// <param name="cancellationToken">İptal jetonu.</param>
    /// <returns>İzin veya kararlı gerekçe kodu taşıyan ret.</returns>
    Task<SelectedContextDecision> AuthorizeAsync(string operationName, CancellationToken cancellationToken = default);
}
