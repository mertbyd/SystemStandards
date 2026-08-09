namespace SystemStandards.Authorization.Contracts.FailureCodes;

/// <summary>
/// Authorization kararının reddedilme gerekçelerinin kararlı kodları.
/// Kodlar authorization server'ın ürettikleriyle birebir aynıdır: ret bir hata değil,
/// sorunun cevabıdır ve gerekçesi bu sabitlerle taşınır.
/// </summary>
public static class SelectedContextFailureCodes
{
    /// <summary>
    /// Seçili tenant/OU/scope kararının reddedilme gerekçeleri.
    /// </summary>
    public static class SelectedContext
    {
        /// <summary>Selected-context hata kodlarının ortak prefix'i.</summary>
        public const string Prefix = "Authenticator.SelectedContext";

        /// <summary>İstenen tenant/OU/scope üçlüsü için kayıt bulunamadı.</summary>
        public const string NotFound = $"{Prefix}:00001";

        /// <summary>Hedef kiracı yok veya pasif.</summary>
        public const string TenantInactive = $"{Prefix}:00002";

        /// <summary>Seçili organizasyon birimi pasif.</summary>
        public const string OrganizationUnitInactive = $"{Prefix}:00003";

        /// <summary>Seçili application scope pasif.</summary>
        public const string ScopeInactive = $"{Prefix}:00004";

        /// <summary>Actor'ün bu bağlamda aktif üyeliği yok.</summary>
        public const string MembershipRequired = $"{Prefix}:00005";

        /// <summary>Actor'ün bu bağlamda aktif rol ataması yok.</summary>
        public const string RoleAssignmentRequired = $"{Prefix}:00006";

        /// <summary>Rol seti operasyon iznini karşılamıyor.</summary>
        public const string OperationPermissionRequired = $"{Prefix}:00007";

        /// <summary>Privileged actor host allowlist'inde bulunmuyor.</summary>
        public const string HostAdministrationNotAllowed = $"{Prefix}:00008";

        /// <summary>Privileged akış gerekçe olmadan çalıştırılamaz.</summary>
        public const string ReasonRequired = $"{Prefix}:00009";

        /// <summary>Privileged akış correlation kimliği olmadan çalıştırılamaz.</summary>
        public const string CorrelationRequired = $"{Prefix}:00010";

        /// <summary>
        /// Authoritative karar kaynağı okunamadı; karar fail-closed döner.
        /// İstemci tarafında taşıma hatası, timeout, 5xx ve ayrıştırılamayan gövde de bu kodla reddedilir.
        /// </summary>
        public const string AuthorizationSourceUnavailable = $"{Prefix}:00011";

        /// <summary>Aynı tenant/OU/scope üçlüsü için ikinci bir bağlam kurulamaz.</summary>
        public const string ContextAlreadyExists = $"{Prefix}:00012";
    }

    /// <summary>
    /// Doğrulanmış principal'ın platform bağlamına çevrilememesinden doğan gerekçeler.
    /// İstemci sarmalayıcısı bu kodları uzak çağrı yapmadan, token'daki claim'lere bakarak üretir.
    /// </summary>
    public static class AuthContext
    {
        /// <summary>Auth-context hata kodlarının ortak prefix'i.</summary>
        public const string Prefix = "Authenticator.AuthContext";

        /// <summary>Actor türü claim'i token'da bulunmuyor.</summary>
        public const string MissingActor = $"{Prefix}:00001";

        /// <summary>Actor türü tanınmıyor veya kimlik claim'iyle eşleşmiyor.</summary>
        public const string UnsupportedActor = $"{Prefix}:00002";

        /// <summary>Aynı token birden fazla actor kimliği taşıyor.</summary>
        public const string AmbiguousActor = $"{Prefix}:00003";

        /// <summary>Application-scope claim'i bulunmuyor veya çözülemedi.</summary>
        public const string MissingApplicationScope = $"{Prefix}:00004";

        /// <summary>Tenant claim'i bulunmuyor.</summary>
        public const string MissingTenant = $"{Prefix}:00005";

        /// <summary>Tenant claim'i boş olmayan bir kimliğe çözülemedi.</summary>
        public const string MalformedTenant = $"{Prefix}:00006";

        /// <summary>Organizasyon birimi claim'i bulunmuyor.</summary>
        public const string MissingOrganizationUnit = $"{Prefix}:00007";

        /// <summary>Organizasyon birimi claim'i boş olmayan bir kimliğe çözülemedi.</summary>
        public const string MalformedOrganizationUnit = $"{Prefix}:00008";

        /// <summary>Token scope'u bu resource server için geçerli değil.</summary>
        public const string InvalidScope = $"{Prefix}:00009";

        /// <summary>Bağlam okunamadı; çağrı hiçbir ambient değere düşmeden reddedilir.</summary>
        public const string Rejected = $"{Prefix}:00010";
    }
}
