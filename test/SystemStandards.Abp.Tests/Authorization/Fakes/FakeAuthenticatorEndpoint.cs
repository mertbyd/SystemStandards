using System.Net;
using System.Text;
using System.Text.Json;
using SystemStandards.Authorization.Contracts.Contracts;
using SystemStandards.Results;

namespace SystemStandards.Abp.Authorization.Tests.Fakes;

/// <summary>
/// Uzak karar ucunun yerine geçen sahte uç.
/// Named client'ın primary handler'ı olarak takılır: istekler gerçek HTTP hattından geçip
/// resilience pipeline'ını da çalıştırır, ağa çıkmaz.
/// </summary>
internal sealed class FakeAuthenticatorEndpoint : HttpMessageHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly Lock _sync = new();
    private readonly List<RecordedAuthorizationRequest> _requests = [];

    /// <summary>Uca ulaşan istekler.</summary>
    public IReadOnlyList<RecordedAuthorizationRequest> Requests
    {
        get
        {
            lock (_sync)
            {
                return _requests.ToList();
            }
        }
    }

    /// <summary>Yanıt üretici; varsayılan olarak izin verir.</summary>
    public Func<RecordedAuthorizationRequest, HttpResponseMessage> Respond { get; set; } = _ => Allowed();

    /// <summary>Yanıttan önce beklenecek süre; timeout senaryosu için kullanılır.</summary>
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var recorded = await RecordedAuthorizationRequest.CaptureAsync(request, cancellationToken);

        lock (_sync)
        {
            _requests.Add(recorded);
        }

        if (Delay > TimeSpan.Zero)
        {
            await Task.Delay(Delay, cancellationToken);
        }

        return Respond(recorded);
    }

    /// <summary>İzin veren karar yanıtı üretir.</summary>
    public static HttpResponseMessage Allowed(
        long contextVersion = 42,
        string applicationScopeLevelCode = "OrganizationUnit",
        bool requiresPrivilegedAudit = false)
        => Envelope(new AuthorizationDecisionContract
        {
            IsAllowed = true,
            FailureCode = null,
            ContextVersion = contextVersion,
            ApplicationScopeLevelCode = applicationScopeLevelCode,
            RequiresPrivilegedAudit = requiresPrivilegedAudit
        });

    /// <summary>Reddeden karar yanıtı üretir; ret bir hata değildir, HTTP 200 döner.</summary>
    public static HttpResponseMessage Denied(string failureCode)
        => Envelope(new AuthorizationDecisionContract
        {
            IsAllowed = false,
            FailureCode = failureCode,
            ContextVersion = 0,
            ApplicationScopeLevelCode = null,
            RequiresPrivilegedAudit = false
        });

    /// <summary>Verilen HTTP durum koduyla gövdesiz yanıt üretir.</summary>
    public static HttpResponseMessage Status(HttpStatusCode statusCode)
        => new(statusCode);

    /// <summary>JSON olarak ayrıştırılamayan bir gövde döner.</summary>
    public static HttpResponseMessage Unparsable()
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>bu bir karar degil</html>", Encoding.UTF8, "application/json")
        };

    /// <summary>SystemStandards Result olmayan paralel bir response zarfı üretir.</summary>
    public static HttpResponseMessage NonStandardEnvelope()
    {
        var payload = JsonSerializer.Serialize(new
        {
            success = true,
            data = new AuthorizationDecisionContract { IsAllowed = true }
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
    }

    // Karari, sunucunun tel uzerindeki Result<T> zarfina sarar.
    private static HttpResponseMessage Envelope(AuthorizationDecisionContract decision)
    {
        var payload = JsonSerializer.Serialize(
            Result<AuthorizationDecisionContract>.Success(decision),
            SerializerOptions);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
    }
}
