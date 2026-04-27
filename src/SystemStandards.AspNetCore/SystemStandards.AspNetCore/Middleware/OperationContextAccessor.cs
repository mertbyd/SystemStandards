namespace SystemStandards.Middleware;

/// <summary>
/// OperationContext'i AsyncLocal üzerinde saklayan thread-safe implementation.
/// Her async flow'un kendi context'i vardır (no cross-request pollution).
/// Singleton olarak register edilir — AsyncLocal zaten per-flow izolasyon sağlar.
/// B11 fix: Scoped DI + static AsyncLocal çelişkisi giderildi (Singleton).
/// </summary>
public class OperationContextAccessor : IOperationContextAccessor
{
    /// <summary>
    /// AsyncLocal — thread-safe, async-aware storage.
    /// Her async flow'un kendi değeri vardır.
    /// static olması Singleton ile uyumlu — field lifetime app ömrü boyunca.
    /// </summary>
    private static readonly AsyncLocal<OperationContext?> _context = new();

    /// <summary>
    /// Mevcut request'in OperationContext'ini al.
    /// Middleware tarafından ayarlanmış değeri döner (null olabilir).
    /// </summary>
    public OperationContext? GetContext() => _context.Value;

    /// <summary>
    /// Yeni OperationContext ayarla (middleware start'ta çağrılır).
    /// </summary>
    public void SetContext(OperationContext context) => _context.Value = context;
}
