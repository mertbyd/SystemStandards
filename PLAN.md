# SystemStandards — Profesyonelleştirme Planı

> **Hedef:** SystemStandards'ı, **her ABP projesinde drop-in olarak kullanılabilecek**, profesyonel kalitede bir Result + ExceptionHandling + Logging + Validation + Middleware kütüphane suite'ine dönüştür.
>
> **Bu doküman Sonnet için yazıldı.** Implementasyonu yapacak olan Sonnet bu dosyayı baştan sona okuyup adım adım uygulayacak. Karar ve trade-off'lar zaten alındı — Sonnet'in işi yorumlamak değil, **birebir uygulamak**.

---

## 0. ZORUNLU OKUMA — Kod Yazma Kuralları

Sonnet bu kuralların **dışına çıkmayacak**. CLAUDE.md'den çekilmiştir.

### 0.1 Dil ve Stil
- **XML doc comment'ler Türkçe** yazılır (mevcut kodla tutarlı kal).
- Kod içi açıklayıcı kısa yorumlar Türkçe olabilir; ama gereksiz yorum yazma.
- **Identifier'lar İngilizce** (PascalCase types, camelCase locals).
- C# nullable reference types açık, implicit usings açık.

### 0.2 No Hardcoding (CRITICAL)
- ResultStatus → HTTP code, ResultStatus → ABP error code, exception type → ResultStatus eşlemeleri **kesinlikle kod içinde switch/if-else olarak yazılmaz**.
- Hepsi `IOptions<T>` pattern'i ile `appsettings.json`'dan veya extension point'lerden gelir.
- "Default" değerler bile `Options` sınıflarının içinde **değiştirilebilir** olarak durmalı.

### 0.3 ABP Framework Critical Rule (CRITICAL)
- **ABP'nin sağladığı bir servis/interface/extension/modül varsa, custom implementation YASAKTIR.**
- Sırasıyla şunları kullan:
  | İhtiyaç | ABP Servisi | Custom Yazma |
  |---|---|---|
  | User identity | `ICurrentUser`, `AbpClaimTypes.UserId` | "sub" claim string'i çekme |
  | Localization | `IStringLocalizer<T>` | Hardcoded dictionary |
  | Logging | `ILogger<T>` | Custom logger |
  | Exception type → status | `IHttpExceptionStatusCodeFinder` | Custom switch |
  | Exception → error info | `IExceptionToErrorInfoConverter` | Custom converter |
  | Cache | `IDistributedCache` | Custom cache |
  | Event Bus | `ILocalEventBus` | Custom publisher |
- ABP'nin sağladığı şeylerin **üzerine** ek katman ekleyebilirsin (extend), ama **yerine geçmek YASAK**.

### 0.4 Security
- User identity DTO'dan alınmaz, **her zaman** `CurrentUser` veya server-side claim'den.
- Authorization Domain Service'te yapılır, AppService'te değil.
- Operation context AsyncLocal'de tutulur, request-bound olmak zorunda.

### 0.5 Genel
- Test projeleri xUnit + WebApplicationFactory ile entegrasyon testleri içerecek.
- Her public type'a Türkçe XML doc.
- DRY prensibi — Result ve Result\<T\> arasında metod kopyalama YASAK.

---

## 1. MEVCUT DURUM (Audit Özeti)

### 1.1 Doğru Yapılmış Olanlar (Korunacak)
- `IResult` + `Result` + `Result<T>` + `ResultStatus` ana yapısı (Ardalis modeli ✅).
- `ValidationError` record (Identifier, Message, ErrorCode, Severity).
- `IResultStatusMapper` abstraction'ı (ama implementasyon refactor edilecek).
- `OperationContext` (CorrelationId, UserId, TenantId) yapısı.
- `Fluent API`: `WithCorrelationId`, `WithLocation`.

### 1.2 Kritik Hatalar (Mutlaka Düzeltilecek)

| # | Dosya | Sorun | Etki |
|---|---|---|---|
| **B1** | `GlobalExceptionMiddleware.cs:81-91` | Exception type'ları **string switch** ile hardcoded | "No hardcoding" ihlali, her yeni exception kod değişikliği gerektiriyor |
| **B2** | `FluentValidationExtensions.cs:155-167` | Türkçe mesajlar **kod içi dictionary** | `IValidationMessageProvider` var ama **kullanılmıyor**, ABP `IStringLocalizer` da yok |
| **B3** | `OperationContextMiddleware.cs:39` | `User.FindFirst("sub")` ile UserId çekiliyor | ABP/OpenIddict'te user ID `AbpClaimTypes.UserId`'da → **her zaman null**, log'da "anonymous" görünüyor |
| **B4** | `AspNetCoreServiceExtensions.cs:43-45` | Middleware sırası: Logging → Exception → Context | Context henüz set edilmeden Logging ve Exception çalışıyor → **CorrelationId: N/A** |
| **B5** | `appsettings.json` | `SystemStandards:ResultStatusMapping` section'ı **yok** | Mapper her çağrıda warning + default 500 dönüyor |
| **B6** | `IResult.cs:55` | `Type ValueType` interface'te zorunlu | `[JsonIgnore]` patch'i var ama tasarım bozuk; `Type` contract'a ait değil |
| **B7** | `Result<T>.cs:20` | `new Type ValueType` shadow | Polymorphism kırık |
| **B8** | `ResultT.cs` (tamamı) | Result<T> tüm factory'leri Result'tan **kopyalanmış** | DRY ihlali, 10+ method 2x duplicate |
| **B9** | `ResultStatusMapper.cs:36` | Her çağrıda `List.FirstOrDefault` linear search | O(n) per request, cache yok |
| **B10** | `ResultToActionResultFilter.cs:81` | `Created` status'ta Location header **eklenmiyor** | RFC 7231 ihlali, comment "context'e erişimi yok" yanlış |
| **B11** | `OperationContextAccessor.cs:13` | Scoped DI + static AsyncLocal field çelişkisi | Lifetime confusion |
| **B12** | Tüm csproj'lar | `<PackageId>`, `<Description>`, `<Authors>`, `<RepositoryUrl>` yok | NuGet'e push edilemez |

### 1.3 Eksik Olan Profesyonel Özellikler

- **ABP Module entegrasyonu YOK** → 4. paket olarak `SystemStandards.Abp` eklenmeli.
- **`ProblemDetails` (RFC 7807) desteği yok** — endüstri standardı.
- **Ardalis-style `TranslateResultToActionResultAttribute`** yok — sadece global filter var.
- **`ResultConvention` Swagger entegrasyonu yok** — `[ProducesResponseType]` otomatik üretilmiyor.
- **`PagedResult<T>`** yok — pagination Result API'sinin parçası değil.
- **`ErrorList`** yok — multiple error aggregation tipi yok.
- **Monadic API (`Map`, `Bind`, `Filter`)** yok — composition zayıf.
- **Test suite tamamen boş**.
- **W3C Trace Context (`Activity.Current.TraceId`)** yerine custom `X-Correlation-Id` — OpenTelemetry uyumsuz.
- **Implicit `T → Result<T>` operator** var ama `null` kontrolü yok.

---

## 2. REFERANS PATTERNLER (Tasarım Kaynakları)

Implementasyonda bu pattern'lere **birebir uy**.

### 2.1 Ardalis.Result Pattern'i (ana model)
**Repo:** https://github.com/ardalis/Result

#### Core sınıflar (mutlaka çoğaltılacak):
```
Result.cs               — generic Result<T>
Result.Void.cs          — non-generic Result (sadece status, value yok)
IResult.cs              — minimal contract (Type ValueType YOK)
ResultStatus.cs         — enum
ValidationError.cs      — record
ValidationSeverity.cs   — enum
ErrorList.cs            — multiple error helper
PagedResult.cs          — pagination result
PagedInfo.cs            — page metadata (page, size, total)
IResultExtensions.cs    — fluent extensions
ResultExtensions.cs     — Map, Bind, Filter (monadic)
```

#### AspNetCore sınıfları (mutlaka çoğaltılacak):
```
TranslateResultToActionResultAttribute.cs
    — ActionFilterAttribute, OnActionExecuted'da intercept
    — controller.ToActionResult(result) çağırır
ActionResultExtensions.cs
    — ToActionResult(this ControllerBase, IResult)
    — switch (status) → CreatedAtRoute / NoContent / Custom
ResultStatusMap.cs
    — Dictionary<ResultStatus, ResultStatusOptions>
    — Fluent API: AddDefaultMap, For(status, code, options).With(responseGen)
    — HTTP method bazlı override (GET 200, POST 201)
ResultStatusOptions.cs
    — DefaultStatusCode, MethodToStatusCodeMap, ResponseTypeFactory
MvcOptionsExtensions.cs
    — AddDefaultResultConvention, AddResultConvention(Action<ResultStatusMap>)
ResultConvention.cs
    — IActionModelConvention
    — TranslateResultToActionResultAttribute olan action'lara otomatik [ProducesResponseType] ekler
    — Swagger/OpenAPI doğru görünür
MinimalApiResultExtensions.cs
    — Minimal API için ToHttpResult
```

### 2.2 ABP Framework Pattern'i (entegrasyon hedefi)

#### Exception Handling Chain (kullanılacak):
```
AbpExceptionFilter (IAsyncExceptionFilter)
    ↓
IExceptionToErrorInfoConverter           — exception → RemoteServiceErrorInfo
IHttpExceptionStatusCodeFinder           — exception → HTTP code
IAbpAuthorizationExceptionHandler        — auth-specific
IExceptionNotifier                        — broadcast

AbpExceptionHandlingMiddleware (middleware seviyesi)
    ↓
context.Items["_AbpActionInfo"] (AbpActionInfoInHttpContext) ile koordinasyon
IsObjectResult ise JSON döner, değilse re-throw
```

#### Standard Error Contract (`RemoteServiceErrorInfo`):
ABP 9.x'te yeri değişmiş, ama API yüzeyi şu (referans):
```csharp
public class RemoteServiceErrorInfo
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    public string? Details { get; set; }
    public object? Data { get; set; }
    public RemoteServiceValidationErrorInfo[]? ValidationErrors { get; set; }
}

public class RemoteServiceValidationErrorInfo
{
    public string Message { get; set; }
    public string[] Members { get; set; }
}
```
**SystemStandards'ın error response'u bu contract'a UYACAK** — ABP frontend'leri (Angular, Blazor, MAUI) bu yapıyı parse ediyor.

#### `WrapResultFilter` (ABP 9'da kaldırılmış)
ABP yeni sürümde otomatik wrap yapmıyor, **`ProblemDetails`** standardına yöneldi. Biz de aynı yolu izleyeceğiz: success'te kendi envelope, error'da `ProblemDetails`.

---

## 3. HEDEFLENEN MİMARİ

### 3.1 Paket Yapısı (4 paket olacak)

```
SystemStandards.Core
    ├── Results/               — IResult, Result, Result<T>, PagedResult, ResultStatus
    ├── Errors/                — ValidationError, ValidationSeverity, ErrorList
    ├── Mapping/               — IResultStatusMapper, ResultStatusMap (Ardalis tarzı)
    ├── Extensions/            — Monadic ops (Map, Bind, Filter)
    └── Options/               — ResultStatusMappingOptions

SystemStandards.Validation
    ├── FluentValidation/      — Custom rule extensions (NotEmptyLocalized vs)
    ├── Localization/          — IValidationMessageProvider (sadece interface, impl ABP'de)
    └── Extensions/            — DI registration

SystemStandards.AspNetCore
    ├── Filters/
    │   ├── TranslateResultToActionResultAttribute.cs   (Ardalis pattern)
    │   └── GlobalResultFilter.cs                       (opsiyonel global)
    ├── Conventions/
    │   └── ResultConvention.cs                         (Swagger için)
    ├── Middleware/
    │   ├── OperationContextMiddleware.cs
    │   ├── RequestResponseLoggingMiddleware.cs
    │   └── ProblemDetailsMiddleware.cs                 (yeni — RFC 7807)
    ├── Context/
    │   ├── IOperationContextAccessor.cs
    │   ├── OperationContextAccessor.cs
    │   └── OperationContext.cs
    └── Extensions/             — UseSystemStandardsAspNetCore

SystemStandards.Abp                                     [YENİ PAKET]
    ├── SystemStandardsAbpModule.cs                     (AbpModule, DependsOn)
    ├── ExceptionHandling/
    │   ├── ResultBasedExceptionToErrorInfoConverter.cs (IExceptionToErrorInfoConverter impl)
    │   └── AbpResultStatusFromExceptionFinder.cs       (exception → ResultStatus mapping options'tan)
    ├── Wrappers/
    │   └── AbpResultWrapAttribute.cs                   (ABP-aware wrap, override edilebilir)
    ├── Localization/
    │   └── AbpValidationMessageProvider.cs             (IStringLocalizer kullanır)
    ├── Identity/
    │   └── AbpOperationContextEnricher.cs              (ICurrentUser → OperationContext)
    └── Extensions/             — ABP DI registration
```

### 3.2 Pipeline (Doğru Sıra)

```
Request
  ↓
[1] OperationContextMiddleware       — CorrelationId, başlangıç zamanı
  ↓
[2] RequestResponseLoggingMiddleware — log start (CorrelationId artık DOLU)
  ↓
[3] ProblemDetailsMiddleware         — yakalanmamış exception'lar için son güvence
  ↓
[ABP'nin kendi middleware'leri: AbpExceptionHandlingMiddleware, vs.]
  ↓
[ASP.NET Core Authentication]
  ↓
[4] AbpOperationContextEnricher      — User dolduğunda UserId set et (post-auth)
  ↓
[Routing → Endpoint → Action]
  ↓
[Action returns IResult]
  ↓
[5] TranslateResultToActionResultAttribute — IResult → ActionResult
  ↓
[Response yazılır, log finish, header'a CorrelationId eklenir]
```

**KRİTİK:** Authentication middleware'inden **sonra** çalışan ikinci bir context-enricher var (4. adım). Çünkü `User.Claims` authentication'dan sonra dolar, OperationContextMiddleware (1. adım) çalıştığında henüz boştur.

---

## 4. UYGULAMA FAZLARI

> Sonnet bu fazları **sırayla** uygulayacak. Her faz bağımsız commit'lenebilir olmalı.

### FAZ 0: Hazırlık ve Temizlik

**Süre:** ~30 dk

#### 0.1 csproj metadata ekle (her 4 paket için)
Her `*.csproj` dosyasına:
```xml
<PropertyGroup>
  <PackageId>SystemStandards.Xxx</PackageId>
  <Version>2.0.0</Version>
  <Authors>Mert Bayd</Authors>
  <Description>Result pattern + ABP Framework integration for .NET 10</Description>
  <PackageTags>result;ardalis;abp;aspnetcore;validation</PackageTags>
  <RepositoryUrl>https://github.com/mertbayd/SystemStandards</RepositoryUrl>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

#### 0.2 Dummy `Class1.cs` dosyalarını sil
Her pakette `Class1.cs` boş template dosyaları var, hepsini sil.

#### 0.3 4. paketi oluştur: `SystemStandards.Abp`
```
src/SystemStandards.Abp/SystemStandards.Abp/
    SystemStandards.Abp.csproj
    SystemStandardsAbpModule.cs (placeholder)
```
csproj package referansları:
- `Volo.Abp.AspNetCore.Mvc` (latest)
- `Volo.Abp.Identity` (sadece ICurrentUser için)
- `Volo.Abp.Validation` (IStringLocalizer için)
- proje referansı: SystemStandards.AspNetCore

#### 0.4 `.slnx`'e SystemStandards.Abp ekle.

#### 0.5 Test projelerine xUnit + Microsoft.AspNetCore.Mvc.Testing referansları ekle.

---

### FAZ 1: Core Refactor

**Süre:** ~2 saat

#### 1.1 `IResult.cs` — `ValueType` kaldır
**Dosya:** `src/SystemStandards.Core/SystemStandards.Core/Results/IResult.cs`

`Type ValueType { get; }` interface'ten **çıkar**. `[JsonIgnore]` patch'leri de gerekmeyecek.
`object? GetValue()` kalır.

#### 1.2 `Result<T>` factory metodlarını `Result`'tan inherit et
**Sorun:** `Result<T>` her factory'yi yeniden yazıyor.
**Çözüm:** `Result` factory'leri `protected static` helper'a çek, `Result<T>` çağırsın.

```csharp
// Result.cs içinde
protected static TResult CreateError<TResult>(ResultStatus status, params string[] errors)
    where TResult : Result, new()
    => new() { Status = status, Errors = errors.ToList() };
```

`Result<T>.NotFound`, `Result<T>.Forbidden` vs. hepsi bu helper'ı kullansın. Kopyala-yapıştır kalkacak.

#### 1.3 `Result<T>` implicit operator null-safe
```csharp
public static implicit operator Result<T>(T value)
    => value is null ? NotFound() : Success(value);
```
Veya çok daha agresif: `null` → `Error`. Karar: **`null` → `NotFound`** (REST anlamlı).

#### 1.4 `ErrorList` ekle
**Yeni dosya:** `Errors/ErrorList.cs`
```csharp
public class ErrorList : List<string>
{
    public string CorrelationId { get; init; } = string.Empty;
    public ErrorList(IEnumerable<string> errors, string correlationId)
        : base(errors) { CorrelationId = correlationId; }
}
```
`Result.Error(ErrorList)` factory'si eklensin.

#### 1.5 `PagedResult<T>` + `PagedInfo` ekle
**Yeni dosyalar:** `Results/PagedResult.cs`, `Results/PagedInfo.cs`
```csharp
public class PagedInfo
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
}

public class PagedResult<T> : Result<T>
{
    public PagedInfo PagedInfo { get; init; } = default!;
    // factory: ToPagedResult(this Result<T>, PagedInfo)
}
```

#### 1.6 Monadic Extensions
**Yeni dosya:** `Extensions/ResultExtensions.cs`
```csharp
public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper);
public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<TOut>> mapper);
public static Result<T> Filter<T>(this Result<T> result, Func<T, bool> predicate, string failureMessage);
public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder);
```
Sonnet bunları yazarken **null guards** koyacak, generic constraints minimal olacak.

#### 1.7 `ResultStatus` enum — gözden geçir
- Mevcut 11 status iyi.
- **Eklenmesi gereken:** `Created` zaten var ama opsiyonel olarak HTTP method bazlı override desteği `ResultStatusOptions`'ta olacak (Ardalis pattern).

---

### FAZ 2: Mapping Refactor (Ardalis-style)

**Süre:** ~3 saat

#### 2.1 `ResultStatusMap` sınıfı oluştur
**Yeni dosya:** `Mapping/ResultStatusMap.cs`

Ardalis pattern'i birebir:
```csharp
public class ResultStatusMap
{
    private readonly Dictionary<ResultStatus, ResultStatusOptions> _map = new();

    public ResultStatusMap AddDefaultMap()
    {
        // Ok→200, Created→201, NoContent→204, Invalid→400,
        // Unauthorized→401, Forbidden→403, NotFound→404,
        // Conflict→409, Error→500, CriticalError→500, Unavailable→503
        return this;
    }

    public ResultStatusMap For(ResultStatus status, HttpStatusCode httpCode,
        Action<ResultStatusOptions>? configure = null) { ... }

    public ResultStatusMap Remove(ResultStatus status) { ... }

    public ResultStatusOptions this[ResultStatus status] => _map[status];

    public bool ContainsKey(ResultStatus status) => _map.ContainsKey(status);
}

public class ResultStatusOptions
{
    public HttpStatusCode DefaultStatusCode { get; set; }
    public Dictionary<string /*HTTP method*/, HttpStatusCode> MethodToStatusCodeMap { get; set; } = new();
    public Func<IResult, object>? ResponseFactory { get; set; }

    public ResultStatusOptions For(string httpMethod, HttpStatusCode code) { ... }
    public ResultStatusOptions With(Func<IResult, object> factory) { ... }
}
```

#### 2.2 `IResultStatusMapper` refactor
- Linear `List.FirstOrDefault` yerine `Dictionary` kullansın.
- `MapToHttpStatusCode(ResultStatus, string httpMethod = "GET")` overload eklensin.
- `ResultStatusMap` instance'ı DI'dan gelsin (`IOptions<ResultStatusMappingOptions>` yerine direkt `ResultStatusMap` singleton).

#### 2.3 `ResultStatusMappingOptions` → `ResultStatusMap`'e dönüştür
Eski options sınıfı silinecek. `appsettings.json`'dan okuma yerine **fluent registration** olacak:

```csharp
// Program.cs'te
services.AddSystemStandardsCore(map => {
    map.AddDefaultMap()
       .For(ResultStatus.Conflict, HttpStatusCode.Conflict)
       .For(ResultStatus.Error, HttpStatusCode.InternalServerError, opts =>
            opts.For("POST", HttpStatusCode.UnprocessableEntity));
});
```

> **Not:** "appsettings tabanlı mapping" CLAUDE.md'de istenmişti ama Ardalis fluent API daha güçlü ve type-safe. Bu yüzden hibrit: fluent default + appsettings opsiyonel override.

#### 2.4 ABP error code mapping ayrı kalsın
`StatusToAbpErrorCodeMap` opsiyonu SystemStandards.Abp paketine taşınsın (Core'da ABP bağımlılığı olmamalı).

---

### FAZ 3: AspNetCore Refactor (Ardalis Pattern)

**Süre:** ~4 saat

#### 3.1 `TranslateResultToActionResultAttribute` ekle
**Yeni dosya:** `Filters/TranslateResultToActionResultAttribute.cs`

```csharp
public class TranslateResultToActionResultAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is not ObjectResult { Value: IResult result })
            return;

        if (context.Controller is not ControllerBase controller)
            return;

        context.Result = controller.ToActionResult(result);
    }
}
```

#### 3.2 `ActionResultExtensions` ekle
**Yeni dosya:** `Extensions/ActionResultExtensions.cs`

`ToActionResult(this ControllerBase, IResult)` — `ResultStatusMap`'ten okuyacak.

```csharp
public static ActionResult ToActionResult(this ControllerBase controller, IResult result)
{
    var map = controller.HttpContext.RequestServices.GetRequiredService<ResultStatusMap>();
    var options = map[result.Status];
    var httpMethod = controller.HttpContext.Request.Method;
    var statusCode = options.MethodToStatusCodeMap.GetValueOrDefault(httpMethod, options.DefaultStatusCode);

    // Created için Location header'ı YA Z (eski filter bunu yapmıyordu!)
    if (result.Status == ResultStatus.Created && !string.IsNullOrWhiteSpace(result.Location))
    {
        controller.HttpContext.Response.Headers.Location = result.Location;
    }

    var responseBody = options.ResponseFactory?.Invoke(result) ?? BuildDefaultBody(result);
    return new ObjectResult(responseBody) { StatusCode = (int)statusCode };
}
```

#### 3.3 `ResultConvention` ekle (Swagger için)
**Yeni dosya:** `Conventions/ResultConvention.cs`

`IActionModelConvention` impl. `TranslateResultToActionResultAttribute` olan action'lara otomatik `[ProducesResponseType]` ekler — `ResultStatusMap`'teki tüm status'ları gezerek.

#### 3.4 `MvcOptionsExtensions` ekle
```csharp
public static MvcOptions AddDefaultResultConvention(this MvcOptions options) { ... }
public static MvcOptions AddResultConvention(this MvcOptions options, Action<ResultStatusMap> configure) { ... }
```

#### 3.5 `GlobalExceptionMiddleware` → `ProblemDetailsMiddleware`
**Eski:** `Middleware/GlobalExceptionMiddleware.cs` — sil
**Yeni:** `Middleware/ProblemDetailsMiddleware.cs`

İçerik:
- Yakalanmamış exception → RFC 7807 `ProblemDetails`
- Exception type → ResultStatus mapping **DI'dan** gelir (`IExceptionToResultStatusResolver`)
- Hardcoded switch YASAK

```csharp
public interface IExceptionToResultStatusResolver
{
    ResultStatus Resolve(Exception exception);
}
```

ABP paketinde implementasyonu olacak. Core'da default basit impl var:
```csharp
public class DefaultExceptionToResultStatusResolver : IExceptionToResultStatusResolver
{
    private readonly Dictionary<Type, ResultStatus> _map;
    public DefaultExceptionToResultStatusResolver(IOptions<ExceptionMappingOptions> options) { ... }
    public ResultStatus Resolve(Exception exception)
    {
        var type = exception.GetType();
        while (type != null) {
            if (_map.TryGetValue(type, out var status)) return status;
            type = type.BaseType;
        }
        return ResultStatus.Error;
    }
}
```

`ExceptionMappingOptions`'a default'lar:
- `ArgumentException` → Invalid
- `UnauthorizedAccessException` → Unauthorized
- `TimeoutException` → Unavailable
- `NotImplementedException` → Error
- vb.

ABP exception'ları (`AbpAuthorizationException`, `EntityNotFoundException`, `BusinessException`) **ABP paketinde** type referansı ile eklenecek (string match YOK).

#### 3.6 `OperationContextMiddleware` düzelt
**Dosya:** `Middleware/OperationContextMiddleware.cs`

- `User.FindFirst("sub")` → `User.FindFirstValue(ClaimTypes.NameIdentifier)` (ABP paketinde `AbpClaimTypes.UserId` ile override).
- W3C Trace Context: `Activity.Current?.TraceId.ToString()` correlation source olarak kullanılsın, header sadece fallback.

#### 3.7 Middleware sırası düzelt
**Dosya:** `Extensions/AspNetCoreServiceExtensions.cs`

```csharp
public static IApplicationBuilder UseSystemStandardsAspNetCore(this IApplicationBuilder app)
{
    app.UseMiddleware<OperationContextMiddleware>();      // 1 — context önce
    app.UseMiddleware<RequestResponseLoggingMiddleware>();// 2 — log (CorrelationId dolu)
    app.UseMiddleware<ProblemDetailsMiddleware>();        // 3 — son güvence
    return app;
}
```

ABP paketinde ayrı extension: `app.UseSystemStandardsAbp()` → ABP-aware enrichment + `AbpOperationContextEnricher`'ı `app.UseAuthentication()` SONRASINA ekler.

#### 3.8 `ResultToActionResultFilter` (eski global filter) — SİL
Yerine `TranslateResultToActionResultAttribute` (action-level) ve `GlobalResultFilter` (opt-in global) gelecek.

---

### FAZ 4: ABP Entegrasyonu (Yeni Paket)

**Süre:** ~3 saat

#### 4.1 `SystemStandardsAbpModule.cs`
```csharp
[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpIdentityDomainSharedModule)
)]
public class SystemStandardsAbpModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IExceptionToErrorInfoConverter, ResultBasedExceptionToErrorInfoConverter>();
        context.Services.AddSingleton<IExceptionToResultStatusResolver, AbpExceptionToResultStatusResolver>();
        context.Services.AddScoped<IValidationMessageProvider, AbpValidationMessageProvider>();
        // ... vs
    }
}
```

#### 4.2 `AbpExceptionToResultStatusResolver`
ABP exception type'larını **type referansı** ile mapler — string match YOK:
```csharp
public ResultStatus Resolve(Exception exception) => exception switch
{
    EntityNotFoundException => ResultStatus.NotFound,
    AbpAuthorizationException => ResultStatus.Forbidden,
    AbpValidationException => ResultStatus.Invalid,
    BusinessException be when be.Code?.Contains("Conflict") == true => ResultStatus.Conflict,
    BusinessException => ResultStatus.Conflict,
    _ => _defaultResolver.Resolve(exception)
};
```

#### 4.3 `ResultBasedExceptionToErrorInfoConverter`
ABP'nin `IExceptionToErrorInfoConverter` interface'ini implement et. ABP'nin pipeline'ı bunu çağıracak. SystemStandards'ın `Result.Error/NotFound/...` formatını ABP'nin `RemoteServiceErrorInfo`'suna çevir.

#### 4.4 `AbpValidationMessageProvider`
Hardcoded dictionary YERİNE `IStringLocalizer<TResource>` kullan. ABP'nin localization sistemi.

#### 4.5 `AbpOperationContextEnricher` (middleware)
ABP'nin `ICurrentUser`'ından gerçek UserId, TenantId, Roles çek. `app.UseAuthentication()` SONRASINA eklenir.

#### 4.6 `AbpResultWrapAttribute` — opsiyonel wrap
ABP'nin eski `WrapResultFilter`'ının yerini tutar. Kullanım:
```csharp
[AbpResultWrap] // opt-in
public Result<ProductDto> GetAsync(Guid id) { ... }
```

---

### FAZ 5: Validation Modernize

**Süre:** ~1 saat

#### 5.1 `FluentValidationExtensions` — Hardcoded dictionary kaldır
**Dosya:** `Extensions/FluentValidationExtensions.cs:155-167`

Tüm `GetLocalizedMessage` çağrıları `IValidationMessageProvider`'a delege edilsin. DI'dan `validatorContext.RootContextData` veya custom property üzerinden alınsın.

#### 5.2 `IValidationMessageProvider` — sadece interface, default impl YOK
Default impl SystemStandards.Abp'ye taşınacak (IStringLocalizer kullanan).

#### 5.3 `ValidationMessageProvider` (Core'daki) → SİL
Yerini AbpValidationMessageProvider alacak. Core'da default impl olmamalı (ABP olmadan kullanılırsa user kendi impl'ini ekler).

---

### FAZ 6: Test Suite

**Süre:** ~3 saat

Her test projesi:
- xUnit
- FluentAssertions
- Microsoft.AspNetCore.Mvc.Testing (integration)
- NSubstitute (mocking)

#### 6.1 `SystemStandards.Core.Tests`
- `Result_Should_Return_Ok_When_Success`
- `Result_Should_Map_Status_Correctly`
- `ResultStatusMap_Should_Override_For_Http_Method`
- `ResultExtensions_Map_Should_Transform_Value`
- `ResultExtensions_Map_Should_Skip_On_Failure`
- `PagedResult_Should_Calculate_TotalPages`

#### 6.2 `SystemStandards.AspNetCore.Tests`
- WebApplicationFactory ile gerçek HTTP testleri:
  - Endpoint `Result.Success(dto)` döndürdüğünde 200 + envelope
  - `Result.NotFound()` → 404 + ProblemDetails
  - `Result.Created(dto, "/api/x/1")` → 201 + Location header
  - `Result.Invalid(errors)` → 400 + ValidationProblemDetails
  - Exception fırlatınca → 500 + ProblemDetails (ResultStatus mapping ile)
  - CorrelationId header'ı response'ta dönüyor mu

#### 6.3 `SystemStandards.Validation.Tests`
- FluentValidation extension'ları doğru error code üretiyor mu
- `IValidationMessageProvider` mock'lanınca mesaj geliyor mu

#### 6.4 `SystemStandards.Abp.Tests` [yeni]
- `AbpExceptionToResultStatusResolver` her ABP exception'ı doğru mapliyor mu
- ABP `BusinessException` → `Conflict` mapping
- `EntityNotFoundException` → `NotFound`

---

### FAZ 7: Doc + Sample

**Süre:** ~1 saat

#### 7.1 README.md
- Kurulum (`dotnet add package SystemStandards.AspNetCore`)
- ABP projelerinde kullanım (`DependsOn[typeof(SystemStandardsAbpModule)]`)
- Non-ABP projelerde kullanım
- Konfigürasyon örnekleri
- Karşılaştırma: Ardalis vs ABP vs SystemStandards (ne sağlıyor, ne fark)

#### 7.2 `samples/` klasörü
- `samples/SystemStandards.AbpSample/` — minimal ABP API
- `samples/SystemStandards.PlainAspNetCoreSample/` — ABP'siz minimal API

---

## 5. KRİTİK BUG-FIX ÖNCELİĞİ (P0)

Eğer Sonnet zaman kısıtlıysa, **önce şunları** düzelt — diğerleri sonra:

1. **B3** — `OperationContextMiddleware` UserId="anonymous" — `AbpClaimTypes.UserId` veya `ClaimTypes.NameIdentifier` (ABP paketinde override).
2. **B4** — Middleware sırası: Context → Logging → Exception.
3. **B1** — `GlobalExceptionMiddleware` hardcoded switch → `IExceptionToResultStatusResolver`.
4. **B10** — `Created` status'ta Location header eklenmiyor — düzelt.
5. **B5** — `appsettings.json`'a default `SystemStandards:ResultStatusMapping` ekle (veya fluent registration kullan).

Bu 5'i düzeltmek SystemStandards'ı **production-usable** yapar; geri kalanı kalite ve genişlemedir.

---

## 6. KAPSAM DIŞI (Bu Plana Dahil DEĞİL)

- InventoryTrackingAutomation tarafında yapılacak değişiklikler (controller'larda `Result<T>` dönüşü vs).
- ABP'nin antiforgery sorununu çözmek (`/api/movement-requests` POST 400 — ayrı issue).
- ABP'nin OAuth password grant deprecation'ı.
- Workflow query filter warning'leri.

Bunlar SystemStandards'ı **kullanan** projenin sorunları — kütüphanenin değil.

---

## 7. BAŞARI KRİTERLERİ

Refactor bittiğinde aşağıdakiler SAĞLANMIŞ olacak:

- [ ] `dotnet build` 0 warning, 0 error.
- [ ] `dotnet test` tüm paketlerde geçer.
- [ ] InventoryTrackingAutomation'a `SystemStandardsAbpModule`'ü `DependsOn` ile bağlayınca **çalışır** (örnek endpoint Result döndürür, doğru envelope ile).
- [ ] Logda `UserId: anonymous` yerine **gerçek UserId** görünür.
- [ ] Logda `CorrelationId: N/A` **kalmaz** — her satırda dolu correlation ID.
- [ ] Exception fırlatıldığında response **`ProblemDetails`** formatında döner (RFC 7807).
- [ ] Swagger UI'da action'ların tüm muhtemel response code'ları görünür (`ResultConvention` sayesinde).
- [ ] `Created` endpoint Response'unda `Location` header **var**.
- [ ] Hardcoded exception switch **yok** — hepsi DI/options.
- [ ] Hardcoded validation message dictionary **yok** — `IStringLocalizer` üzerinden.
- [ ] 4 paket de NuGet'e push edilebilir (csproj metadata tam).

---

## 8. SONUNDA TAKLA ATILMAYACAK SORULAR

Sonnet bu kararlara **uyacak** — yeniden müzakere etmeyecek:

| Soru | Karar |
|---|---|
| ProblemDetails mi yoksa ABP RemoteServiceErrorInfo mu? | **İkisi de.** Core'da ProblemDetails (RFC 7807). ABP paketinde converter ile RemoteServiceErrorInfo'ya adapt. Frontend ABP bekliyor, OpenAPI tooling RFC 7807 bekliyor — ikisi de döner. |
| Result `null` value ne döner? | **NotFound.** `null` data anlamlı değil REST'te. |
| Localization Core'da mı ABP'de mi? | **Core sadece interface.** Implementation ABP paketinde (IStringLocalizer). |
| Mapping fluent mi appsettings mi? | **Fluent default + appsettings opsiyonel override.** |
| Wrap her zaman mı, opt-in mi? | **Opt-in attribute** (`TranslateResultToActionResultAttribute`) + opsiyonel global filter. ABP modül endpoint'lerine **karışmayacak**. |
| ABP antiforgery sorunu burada mı çözülecek? | **Hayır.** Plan kapsam dışı, kullanan projenin sorumluluğu. |

---

## 9. SONNET İÇİN BAŞLANGIÇ KOMUTU

Bu plan dosyasını okuduktan sonra Sonnet şu şekilde başlayacak:

```
1. CLAUDE.md'yi oku (kod yazma kurallarını teyit et).
2. Mevcut kaynak kodu Glob + Read ile incele.
3. PLAN.md'deki Faz 0'dan başla.
4. Her fazı bitirince:
   - dotnet build ile derle
   - test varsa dotnet test ile koş
   - Bir sonraki faza geç
5. Faz 7 bittikten sonra başarı kriterlerini check et.
6. Eksik kalan varsa kullanıcıya rapor et — kendi başına devam etme.
```

---

**Plan hazırlayan:** Opus 4.7 (referans inceleme + audit)
**Uygulayacak:** Sonnet
**Tarih:** 2026-04-26
**Versiyon:** 1.0
