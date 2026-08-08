using Microsoft.AspNetCore.Mvc;
using SystemStandards.Abp.Authorization.Authorization;

namespace SystemStandards.Abp.Authorization.Tests.App;

/// <summary>
/// Sarmalayıcıyı gerçek bir HTTP hattında çalıştıran korunan controller.
/// </summary>
[ApiController]
[Route(RoutePrefix)]
public sealed class ProtectedController : ControllerBase
{
    /// <summary>Korunan controller'ın rota öneki.</summary>
    public const string RoutePrefix = "protected";

    /// <summary>Korunan action'ın rotası.</summary>
    public const string ManageRoute = "manage";

    /// <summary>Yalnız karar izin verdiğinde çalışan action.</summary>
    [HttpGet(ManageRoute)]
    [RequireSelectedContext(TestOperationNames.Manage)]
    public IActionResult Manage() => Ok(TestOperationNames.Manage);
}
