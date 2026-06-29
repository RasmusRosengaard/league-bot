namespace LolMatchAlert.Core.Riot;

/// <summary>Basisfejl for alle fejl der stammer fra Riot-API'et.</summary>
public class RiotApiException(string message, int? statusCode = null, Exception? inner = null)
    : Exception(message, inner)
{
    public int? StatusCode { get; } = statusCode;
}

/// <summary>
/// Kastes ved 401/403 — typisk en udløbet development-nøgle (gælder kun 24 timer)
/// eller en nøgle uden adgang til endpointet.
/// </summary>
public sealed class RiotAuthException(string message, int statusCode)
    : RiotApiException(message, statusCode);

/// <summary>
/// Kastes ved 429 efter at resilience-laget har opgivet at vente Retry-After af.
/// </summary>
public sealed class RiotRateLimitException(string message, TimeSpan? retryAfter)
    : RiotApiException(message, 429)
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
