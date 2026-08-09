# SystemStandards.Authorization.Contracts

Dependency-free wire contracts shared by an Authenticator resource server and its
consumers.

```bash
dotnet add package SystemStandards.Authorization.Contracts --version 1.0.0
```

The package contains:

- selected tenant, organization-unit and application-scope claim names;
- privileged request header names;
- authorization request, decision and result-envelope contracts;
- the stable Authenticator endpoint path;
- stable selected-context and authentication failure codes.

It deliberately has no framework or package dependency. Server and client projects
can share the public HTTP contract without sharing an ABP, ASP.NET Core or provider
implementation.

Source: [SystemStandards](https://github.com/mertbyd/SystemStandards)
