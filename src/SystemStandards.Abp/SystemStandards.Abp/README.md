# SystemStandards.Abp

ABP Framework icin SystemStandards Result entegrasyonu, exception esleme ve istege bagli
selected-context authorization istemcisi.

## Authorization kurulumu

Uygulama modulu mevcut `SystemStandardsAbpModule` bagimliligini kullanir. Authorization
istemcisi yalniz ihtiyac duyan uygulamada etkinlestirilir:

```csharp
context.Services.AddAuthenticatorAuthorization(configuration);
```

Korunacak action veya application-service metodu kararlı operasyon adini tasir:

```csharp
[RequireSelectedContext(MyPermissions.Sources.Manage)]
public virtual Task UpdateAsync(...)
```

Token veya zorunlu claim eksikligi, uzak karar kaynagi hatasi, timeout ve olumsuz karar
fail-closed olarak reddedilir.
