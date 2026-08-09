using System.Diagnostics.CodeAnalysis;

namespace SystemStandards.Abp.Authorization.Authorization;

/// <summary>
/// Operasyon adını authorization policy adına çeviren sözleşme.
/// Policy adı hem ASP.NET Core authorization middleware'inin hem de ABP'nin method invocation
/// authorization'ının anladığı tek ortak dildir; ikisi de policy'yi <b>adıyla</b> çözer.
/// </summary>
public static class SelectedContextPolicyNames
{
    /// <summary>Bu sarmalayıcıya ait policy adlarının öneki.</summary>
    public const string Prefix = "SystemStandards.SelectedContext:";

    /// <summary>Operasyon adından policy adı üretir.</summary>
    /// <param name="operationName">İzni sorulan operasyonun kararlı adı.</param>
    public static string Build(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return Prefix + operationName;
    }

    /// <summary>Policy adı bu sarmalayıcıya aitse taşıdığı operasyon adını çıkarır.</summary>
    /// <param name="policyName">Çözülmesi istenen policy adı.</param>
    /// <param name="operationName">Policy adının taşıdığı operasyon adı.</param>
    /// <returns>Policy adı bu sarmalayıcıya aitse true.</returns>
    public static bool TryReadOperationName(
        string? policyName,
        [NotNullWhen(true)] out string? operationName)
    {
        operationName = null;

        if (policyName is null || !policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var candidate = policyName[Prefix.Length..];
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        operationName = candidate;

        return true;
    }
}
