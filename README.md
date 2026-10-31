# SystemStandards

Result-pattern building blocks for ASP.NET Core and ABP Framework projects, released as one
lockstep package family. Every package in the family shares a single version.

## Packages

| Package | Depends on | What it gives you |
|---|---|---|
| `SystemStandards.Core` | — | `IResult`, `Result<T>`, `ResultStatus`, `ResultStatusMap` |
| `SystemStandards.Validation` | Core | FluentValidation integration and localized validation messages |
| `SystemStandards.AspNetCore` | Core | ProblemDetails middleware, `TranslateResultToActionResult`, `OperationContext` |
| `SystemStandards.Abp` | AspNetCore, Validation | ABP module, exception-to-`RemoteServiceErrorInfo` mapping, `ICurrentUser` enrichment |
| `SystemStandards.Authorization.Contracts` | — | Authenticator wire contract: claim names, headers, decision payload, failure codes. Deliberately dependency-free |
| `SystemStandards.Abp.Authorization` | Abp, Authorization.Contracts | Fail-closed tenant / organization-unit / application-scope authorization client over the Authenticator decision endpoint |

`Authorization.Contracts` carries no dependencies on purpose: it is the single truth shared by
the client wrapper and the authorization server, so neither side's framework choice leaks into
the other.

## Install

Most consumers take the layer they need; the rest arrives transitively.

```
dotnet add package SystemStandards.Abp
dotnet add package SystemStandards.Abp.Authorization
```

## Usage

Add the module to your ABP host and register the authorization client:

```csharp
[DependsOn(typeof(SystemStandardsAbpModule))]
public class MyHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAuthenticatorAuthorization();
    }
}
```

`SystemStandardsAbpModule` wraps ABP's `DefaultExceptionToErrorInfoConverter` and fills the
`Code` field with a `ResultStatus` value only when ABP leaves it empty. ABP's own exception
boundary and validation details stay intact.

## Versioning

The family is versioned in lockstep: a release publishes every package at the same version,
even when only one of them changed. This keeps the internal dependency graph consistent and
avoids the drift that piecemeal releases produce.

Published versions are immutable. A change always means a new version.

## License

MIT
