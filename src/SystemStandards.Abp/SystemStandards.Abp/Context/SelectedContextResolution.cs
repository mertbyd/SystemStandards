namespace SystemStandards.Abp.Authorization.Context;

/// <summary>
/// Bağlam okuma denemesinin sonucu.
/// Başarısızlık bir exception değil, kararlı gerekçe kodu taşıyan bir sonuçtur;
/// çağıran bu durumda uzak çağrı yapmadan reddeder.
/// </summary>
public sealed class SelectedContextResolution
{
    private SelectedContextResolution(SelectedContext? context, string? failureCode)
    {
        Context = context;
        FailureCode = failureCode;
    }

    /// <summary>Bağlam okundu mu.</summary>
    public bool IsResolved => Context is not null;

    /// <summary>Okunan bağlam; başarısızlıkta null kalır.</summary>
    public SelectedContext? Context { get; }

    /// <summary>Ret gerekçesinin kararlı kodu; başarıda null kalır.</summary>
    public string? FailureCode { get; }

    /// <summary>Okunmuş bağlamı taşıyan başarılı sonuç üretir.</summary>
    public static SelectedContextResolution Resolved(SelectedContext context)
        => new(context, failureCode: null);

    /// <summary>Verilen gerekçe koduyla başarısız sonuç üretir.</summary>
    public static SelectedContextResolution Failed(string failureCode)
        => new(context: null, failureCode);
}
