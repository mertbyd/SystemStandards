# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

**SystemStandards** is a .NET 10.0 library suite implementing a standardized **Result pattern** for consistent operation outcome handling across applications. It's modeled after Ardalis.Result but tailored for Turkish-language development environments.

### Three-Package Structure

1. **SystemStandards.Core** — Core Result types and status mapping
   - `IResult` interface: contract for all operation results
   - `Result<T>` and `Result` classes: success/failure outcomes
   - `ResultStatus` enum: 11 status codes (Ok, Created, Invalid, NotFound, Unauthorized, Forbidden, Conflict, Error, CriticalError, Unavailable, NoContent)
   - `IResultStatusMapper`: maps `ResultStatus` → HTTP status code (200, 201, 400, 404, 401, 403, 409, 500, 503, 204)
   - `ValidationError` record: structured validation failure with Identifier, ErrorMessage, ErrorCode, Severity

2. **SystemStandards.Validation** — FluentValidation integration
   - Extensions for converting FluentValidation failures → `ValidationError` list
   - Localized validation message provider
   - Bridges validator output into Result pattern

3. **SystemStandards.AspNetCore** — ASP.NET Core plumbing
   - `ResultToActionResultFilter`: IAsyncActionFilter that converts `IResult` → HTTP ActionResult (wraps into JSON with correct status code)
   - `GlobalExceptionMiddleware`: catches unhandled exceptions, converts to Result
   - `OperationContextMiddleware`: injects correlation ID, user context into ambient state
   - `IOperationContextAccessor`: AsyncLocal accessor for per-request context
   - Service extension methods for easy DI registration

### Key Design Principle

**No hardcoding.** All operation outcomes flow through `IResult`, all HTTP codes and error structures are data-driven by `ResultStatus`, `IResultStatusMapper`, and `ValidationError`. Controllers return Results, filters/middleware handle translation to HTTP.

---

## Build & Test Commands

```bash
# Restore packages
dotnet restore

# Build entire solution
dotnet build

# Build specific project (example: Core)
dotnet build src/SystemStandards.Core/SystemStandards.Core/SystemStandards.Core.csproj

# Run all tests (once test projects are populated)
dotnet test

# Run tests for one project
dotnet test test/SystemStandards.Core.Tests/

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"

# Build NuGet packages
dotnet pack

# Publish packages to local nupkg folder
dotnet pack -o nupkg/
```

---

## Project Structure

```
SystemStandards/
├── src/
│   ├── SystemStandards.Core/
│   │   └── SystemStandards.Core/
│   │       ├── Results/
│   │       │   ├── IResult.cs          # Result contract
│   │       │   ├── Result.cs           # Non-generic result
│   │       │   ├── ResultT.cs          # Generic result
│   │       │   └── ResultStatus.cs     # Status enum
│   │       ├── Mapping/
│   │       │   ├── IResultStatusMapper.cs
│   │       │   └── ResultStatusMapper.cs
│   │       ├── Options/
│   │       │   └── ResultStatusMappingOptions.cs
│   │       └── Extensions/
│   │           └── ServiceCollectionExtensions.cs
│   ├── SystemStandards.Validation/
│   │   └── SystemStandards.Validation/
│   │       ├── Extensions/
│   │       │   ├── FluentValidationExtensions.cs
│   │       │   └── ValidationServiceCollectionExtensions.cs
│   │       └── Localization/
│   │           ├── IValidationMessageProvider.cs
│   │           └── ValidationMessageProvider.cs
│   └── SystemStandards.AspNetCore/
│       └── SystemStandards.AspNetCore/
│           ├── Filters/
│           │   └── ResultToActionResultFilter.cs
│           ├── Middleware/
│           │   ├── GlobalExceptionMiddleware.cs
│           │   ├── OperationContextMiddleware.cs
│           │   ├── IOperationContextAccessor.cs
│           │   └── OperationContextAccessor.cs
│           └── Extensions/
│               └── AspNetCoreServiceExtensions.cs
├── test/
│   ├── SystemStandards.Core.Tests/         # Empty placeholder
│   ├── SystemStandards.Validation.Tests/   # Empty placeholder
│   └── SystemStandards.AspNetCore.Tests/   # Empty placeholder
├── nupkg/                                   # NuGet package output
├── SystemStandards.slnx                    # Solution file
└── SystemStandards.sln.DotSettings.user    # Rider settings
```

---

## Core Concepts

### Result Pattern

Every operation returns an `IResult`:
- Success: `Result<T>` with `IsSuccess=true`, status=Ok/Created/NoContent, data payload
- Validation failure: `Result<T>` with `IsSuccess=false`, status=Invalid, `ValidationErrors` list
- Other failure: `Result<T>` with `IsSuccess=false`, status=NotFound/Unauthorized/etc, `Errors` message list

Example flow:
```csharp
public Result<ProductDto> CreateProduct(CreateProductDto dto)
{
    // Validate input (FluentValidation → ValidationError list)
    // If invalid → return Result.Invalid(errors)
    
    // Execute business logic
    // If not found → return Result.NotFound()
    // If success → return Result.Created(data, location: "/api/products/123")
}
```

### ResultStatus Enum

Maps cleanly to HTTP:
- **Success**: Ok (200), Created (201), NoContent (204)
- **Client Error**: Invalid (400), Unauthorized (401), Forbidden (403), NotFound (404), Conflict (409)
- **Server Error**: Error (500), CriticalError (500), Unavailable (503)

### ValidationError Record

Validation failures are **typed** not stringified:
```csharp
public record ValidationError(
    string Identifier,           // "Email" or "Items[0].Name"
    string ErrorMessage,         // Localized user message
    string ErrorCode = "...",    // Programmatic code ("EMAIL_INVALID", "REQUIRED")
    ValidationSeverity Severity  // Error, Warning, Info
);
```

### Operation Context

Per-request ambient state (via AsyncLocal):
- `CorrelationId`: trace across logs
- `UserId` (future): for authorization context
- `IpAddress` (future): for audit

Injected by `OperationContextMiddleware`, accessed via `IOperationContextAccessor`.

---

## Common Workflows

### Adding a New Result Status

1. Add value to `ResultStatus` enum in `src/.../Results/ResultStatus.cs`
2. Update `IResultStatusMapper` implementations to handle new status → HTTP code mapping
3. Update response shape in `ResultToActionResultFilter` if needed (e.g., custom error body)

### Adding Validation to an Operation

1. Create FluentValidator for your DTO (using FluentValidation)
2. Use extension in `SystemStandards.Validation.Extensions.FluentValidationExtensions` to convert failures → `List<ValidationError>`
3. Return `Result.Invalid(validationErrors)` in your operation
4. Filter/middleware handle the rest (correct HTTP 400, response shape)

### Integrating into ASP.NET Core App

```csharp
// Program.cs
builder.Services.AddSystemStandardsCore();           // DI for Result pattern
builder.Services.AddSystemStandardsValidation();    // Validation extensions
builder.Services.AddSystemStandardsAspNetCore();    // Middleware + filters

app.UseSystemStandardsAspNetCore();  // Add to pipeline (order: exception → context → rest)
```

Then in controllers, return `IResult`:
```csharp
[HttpPost]
public async Task<IResult<ProductDto>> Create([FromBody] CreateProductDto dto)
{
    // Your logic returns IResult
    // Filter auto-converts to ActionResult with correct HTTP status
    return await _productService.CreateAsync(dto);
}
```

### Running Tests

Test projects are empty; populate with xUnit or NUnit:
```bash
dotnet test test/SystemStandards.Core.Tests/
dotnet test --filter "ClassName.MethodName"
```

---

## Code Style Notes

- **Language**: C# with Turkish XML documentation comments
- **Frameworks**: .NET 10.0, nullable reference types enabled, implicit usings
- **Dependencies**: Microsoft.Extensions.* (DI, Logging, Localization), FluentValidation
- **Naming**: PascalCase for types, camelCase for locals, descriptive identifiers
- **Comments**: Turkish language in XML docs and code comments

---

## Geçmiş Sürümler & Değişiklikler

### v1.1.0 (2026-04-25)
- **Serialization Fix**: `System.Type` (`ValueType`) özelliği `[JsonIgnore]` ile işaretlenerek login crash sorunu çözüldü.
- **Fluent API**: `Result` sınıflarına `WithCorrelationId` ve `WithLocation` zincirlenebilir metodları eklendi.
- **Dynamic Exception Handling**: `GlobalExceptionMiddleware` artık `IResultStatusMapper` kullanarak appsettings tabanlı hata yönetimi yapıyor.
- **Logging**: `RequestResponseLoggingMiddleware` implemente edildi ve pipeline'a eklendi.
- **Filter Activation**: `ResultToActionResultFilter` aktif edildi ve response formatı standardize edildi.
- **Infrastructure**: `.csproj` dosyaları .NET 10 standartlarına çekildi, obsolete paketler temizlendi.

---

## Known TODOs

- Test projeleri hala boş; kapsamlı test suite yazılmalı
- `ValidationMessageProvider` için resource-based localization desteği (IStringLocalizer)

---

## Development Tips

1. **Result pattern is contract-first**: Start with your operation's return type, then implement
2. **Mapper is your friend**: All HTTP status mappings go through `IResultStatusMapper`, not scattered if-statements
3. **Validation is data**: `ValidationError` records are queryable, serializable, and support multi-severity scenarios
4. **Correlation ID is crucial**: Middleware injects it; always log it for traceability
5. **Middleware order matters**: Exception handler must come first, then context, then the rest

---

## Bağlam Navigasyonu (Wiki-Brain)

`C:\Users\mertb\OneDrive\Belgeler\InventoryWiki` adresinde kişisel bir wiki'ye erişiminiz var. Bu, bilgi birikimi tabanınızdır.

Kodu, dokümentasyonu, geçmiş işleri veya depolanan bilgileri anlamanız gerektiğinde:

1. **HER ZAMAN önce bilgi grafiğini sorgulayın:** `graphify query "sorunuz"`
   (wiki klasöründen çalıştırın).
2. **Wiki yapısını görmek için `C:\Users\mertb\OneDrive\Belgeler\InventoryWiki/wiki/index.md` kullanın.**
3. **Varsa `C:\Users\mertb\OneDrive\Belgeler\InventoryWiki/graphify-out/wiki/index.md` kontrol edin** — otomatik oluşturulan indeks.
4. **`raw/` klasöründeki dosyaları sadece** kullanıcı açıkça söylerse veya grafik sorgusu cevap vermezse okuyun.

## Wiki-Brain Oturum Kuralları

**Kaynakları içeri aktarma.** Kullanıcı `raw/` klasörüne bir dosya koyup `/wiki-brain ingest` istediğinde:
- Kaynağı okuyun
- Özet hazırlayın
- Wiki sayfaları oluşturun/güncelleyin
- `[[Page Name]]` ile çapraz bağlantılar yapın
- `wiki/index.md` güncelleyin
- `log.md` dosyasına satır ekleyin

**Her oturum bir log girişiyle bitmelidir.** Oturum sona ermeden önce bu biçimde yazın:

```
## [YYYY-MM-DD HH:MM] session | <3-8 sözcüklük başlık>
Touched: <wiki sayfaları, veya "none">
```

**Kalıcı bilgi oluşturulduysa** (kararlar, öğrenilen şeyler, durum değişiklikleri, çözülen sorunlar) — ilgili wiki sayfalarını güncelleyin. Bağlantılar yapın.

**Sıradan bir görevse** (tek seferlik onarım, rutin iş) — sadece log satırını ekleyin.

**Kurallar:**
- `raw/` dosyalarını asla değiştirmeyin (kaynaklar sabittir)
- `wiki/` tamamen Claude'a aittir
- Sayfa oluştur/adlandır → `wiki/index.md` güncelleyin
- Çapraz bağlantıları agresif kullanın: `[[Sayfa Adı]]`

## Kullanılabilir Komutlar

- `/wiki-brain` — durum menüsü
- `/wiki-brain ingest <dosya>` — kaynak ekle
- `/wiki-brain query "<soru>"` — grafik + wiki sorgula
- `/wiki-brain lint` — wiki sağlık kontrolü
- `/wiki-brain rebuild` — grafik yeniden oluştur
- `/wiki-brain doctor` — kurulum doğrula
- `/recall` — son 5 aktivite göster

