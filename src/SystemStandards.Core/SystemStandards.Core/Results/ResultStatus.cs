namespace SystemStandards.Results;

/// <summary>
/// Operasyon sonuçlarının durumunu temsil eden enum.
/// Her status, HTTP status code'u ve business kurallarını belirler.
/// Ardalis.Result pattern'ından uyarlanmıştır.
/// </summary>
public enum ResultStatus
{
    /// <summary>İşlem başarılı, veri döndü (HTTP 200 OK)</summary>
    Ok = 0,

    /// <summary>Kaynak oluşturuldu, Location header'ı var (HTTP 201 Created)</summary>
    Created = 1,

    /// <summary>İşlem başarılı, veri yok (HTTP 204 No Content)</summary>
    NoContent = 2,

    /// <summary>Genel hata (HTTP 500 Internal Server Error)</summary>
    Error = 3,

    /// <summary>Doğrulama hatası (HTTP 400 Bad Request)</summary>
    Invalid = 4,

    /// <summary>Kaynak bulunamadı (HTTP 404 Not Found)</summary>
    NotFound = 5,

    /// <summary>Erişim yasaklı (HTTP 403 Forbidden)</summary>
    Forbidden = 6,

    /// <summary>Yetkisiz erişim (HTTP 401 Unauthorized)</summary>
    Unauthorized = 7,

    /// <summary>Kaynak çakışması (HTTP 409 Conflict)</summary>
    Conflict = 8,

    /// <summary>Kritik hata, immediate action gerekli (HTTP 500)</summary>
    CriticalError = 9,

    /// <summary>Hizmet geçici olarak kullanılamaz (HTTP 503 Service Unavailable)</summary>
    Unavailable = 10
}
