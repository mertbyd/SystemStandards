using Microsoft.AspNetCore.Authorization;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Seçili bağlamda bir operasyonun izinli olmasını isteyen authorization requirement.
/// </summary>
public sealed class SelectedContextRequirement : IAuthorizationRequirement
{
    /// <summary>Requirement'ı sorulan operasyon adıyla kurar.</summary>
    /// <param name="operationName">İzni sorulan operasyonun kararlı adı.</param>
    public SelectedContextRequirement(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        OperationName = operationName;
    }

    /// <summary>İzni sorulan operasyonun kararlı adı.</summary>
    public string OperationName { get; }
}
