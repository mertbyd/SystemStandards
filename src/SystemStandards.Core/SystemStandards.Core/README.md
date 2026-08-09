# SystemStandards.Core

SystemStandards Result pattern'inin temel paketidir. `Result<T>`, `Result`, durum esleme,
validation hata modeli ve paylasilan authorization tel sozlesmelerini tasir.

Authorization karar ucu ve istemcisi ayni `Result<AuthorizationDecisionContract>` tipini
kullanir; ikinci bir response zarfina ihtiyac yoktur.
