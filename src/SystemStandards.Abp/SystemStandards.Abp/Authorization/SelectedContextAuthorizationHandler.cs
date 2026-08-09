using Microsoft.AspNetCore.Authorization;
using SystemStandards.Authorization.Contracts.FailureCodes;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// <see cref="SelectedContextRequirement"/> ile <see cref="ISelectedContextAuthorizer"/> arasındaki köprü.
/// İzin verilmeyen her sonuçta açıkça <c>Fail</c> çağırır; sessiz geçiş yoktur.
/// </summary>
public sealed class SelectedContextAuthorizationHandler : AuthorizationHandler<SelectedContextRequirement>
{
    private readonly ISelectedContextAuthorizer _authorizer;

    /// <summary>Bağımlılıkları alır.</summary>
    public SelectedContextAuthorizationHandler(ISelectedContextAuthorizer authorizer)
    {
        _authorizer = authorizer;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SelectedContextRequirement requirement)
    {
        var decision = await _authorizer.AuthorizeAsync(requirement.OperationName);

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);

            return;
        }

        /* Requirement'i basarisiz birakmak yetmez: baska bir handler ayni requirement'i
         * karsilarsa istek gecerdi. Ret kararli gerekce koduyla kesinlestirilir. */
        context.Fail(new AuthorizationFailureReason(
            this,
            decision.FailureCode ?? SelectedContextFailureCodes.AuthContext.Rejected));
    }
}
