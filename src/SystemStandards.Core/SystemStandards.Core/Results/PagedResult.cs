namespace SystemStandards.Results;

/// <summary>
/// Sayfalı sorgu sonuçlarının meta verilerini taşıyan sınıf.
/// Pagination endpoint'lerinde Result ile birlikte döner.
/// </summary>
public class PagedInfo
{
    /// <summary>Sayfa numarası (1'den başlar)</summary>
    public int PageNumber { get; init; }

    /// <summary>Sayfa başına kayıt sayısı</summary>
    public int PageSize { get; init; }

    /// <summary>Toplam kayıt sayısı (filtrelenmemiş)</summary>
    public int TotalRecords { get; init; }

    /// <summary>Toplam sayfa sayısı (hesaplanır)</summary>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling(TotalRecords / (double)PageSize)
        : 0;

    /// <summary>
    /// PagedInfo oluşturur.
    /// </summary>
    /// <param name="pageNumber">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa boyutu</param>
    /// <param name="totalRecords">Toplam kayıt sayısı</param>
    public PagedInfo(int pageNumber, int pageSize, int totalRecords)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRecords = totalRecords;
    }
}

/// <summary>
/// Sayfalı veri döndüren generic operasyon sonucu.
/// Ardalis.Result pattern'ından uyarlanmıştır.
/// Örn: Task&lt;PagedResult&lt;ProductDto&gt;&gt; GetListAsync(pageNumber, pageSize)
/// </summary>
public class PagedResult<T> : Result<T>
{
    /// <summary>Sayfalama meta verisi</summary>
    public PagedInfo PagedInfo { get; init; } = default!;

    /// <summary>
    /// Başarılı sayfalı sonuç oluşturur.
    /// Kullanım: return PagedResult&lt;ProductDto&gt;.Success(items, pagedInfo);
    /// </summary>
    public static PagedResult<T> Success(T data, PagedInfo pagedInfo, string? message = null)
        => new()
        {
            Status = ResultStatus.Ok,
            Value = data,
            PagedInfo = pagedInfo,
            SuccessMessage = message
        };
}

/// <summary>
/// Result&lt;T&gt;'yi PagedResult&lt;T&gt;'ye dönüştüren extension method'lar.
/// </summary>
public static class PagedResultExtensions
{
    /// <summary>
    /// Başarılı Result&lt;T&gt;'yi PagedResult&lt;T&gt;'ye dönüştürür.
    /// Kullanım: result.ToPagedResult(pagedInfo)
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(this Result<T> result, PagedInfo pagedInfo)
    {
        if (result.IsSuccess)
        {
            return PagedResult<T>.Success(result.Value!, pagedInfo, result.SuccessMessage);
        }

        // Hata durumunda: başarısız ama metadata taşıyan boş sonuç
        return PagedResult<T>.Success(default!, new PagedInfo(0, 0, 0));
    }
}
