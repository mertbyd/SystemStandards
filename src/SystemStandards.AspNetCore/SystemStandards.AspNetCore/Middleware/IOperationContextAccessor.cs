namespace SystemStandards.Middleware;

/// <summary>
/// Her HTTP request için unique operation context'i tutar ve retrieve eder.
/// Logging, tracing, user info gibi request-level data'yı store eder.
/// AsyncLocal üzerinden thread-safe bir şekilde çalışır.
/// </summary>
public interface IOperationContextAccessor
{
    /// <summary>
    /// Mevcut request'in OperationContext'ini al
    /// </summary>
    OperationContext? GetContext();

    /// <summary>
    /// Yeni OperationContext ayarla (middleware tarafından çağrılır)
    /// </summary>
    void SetContext(OperationContext context);
}

/// <summary>
/// Her HTTP request'in unique metadata'sını tutar.
/// Correlation ID, user info, timing, vb.
/// </summary>
public class OperationContext
{
    /// <summary>Request benzersiz ID'si (tracing için)</summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Request başlangıç zamanı (elapsed time hesabı için)</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Aktif user ID (ClaimsPrincipal.FindFirst("sub"))</summary>
    public string? UserId { get; set; }

    /// <summary>Tenant ID (multi-tenancy)</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Operation adı (örn: "POST /api/products")</summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>Custom metadata (extension points)</summary>
    public Dictionary<string, object> CustomData { get; set; } = new();
}
