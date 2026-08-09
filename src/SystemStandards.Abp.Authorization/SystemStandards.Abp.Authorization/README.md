# SystemStandards.Abp.Authorization

Fail-closed selected-context authorization for ABP applications.

```bash
dotnet add package SystemStandards.Abp.Authorization --version 1.0.0
```

Add the module dependency and register the Authenticator client configuration:

```csharp
[DependsOn(typeof(SystemStandardsAbpAuthorizationModule))]
public class MyApplicationModule : AbpModule;

services.AddAuthenticatorAuthorization(configuration);
```

Protect controller actions or ABP application-service methods with an operation
name:

```csharp
[RequireSelectedContext("MyProduct.Sources.Manage")]
public Task UpdateAsync() => Task.CompletedTask;
```

The package supplies typed options with startup validation, selected-context claim
access, a resilient HTTP authorizer, and policy integration for both ASP.NET Core
and ABP application-service interception.

Missing or malformed claims, denied decisions, timeouts, transport failures, 5xx
responses and unreadable response envelopes all deny access.

Source: [SystemStandards](https://github.com/mertbyd/SystemStandards)
