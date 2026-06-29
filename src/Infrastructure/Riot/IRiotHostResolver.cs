namespace LolMatchAlert.Infrastructure.Riot;

/// <summary>
/// Oversætter et regionalt host-fragment (f.eks. "europe") til den fulde base-URI.
/// Findes som abstraktion så integrationstests kan pege klienten mod en WireMock-server.
/// </summary>
public interface IRiotHostResolver
{
    Uri Resolve(string regionalHost);
}

/// <summary>Standard-resolver der peger på Riots rigtige API-host.</summary>
public sealed class RiotHostResolver : IRiotHostResolver
{
    public Uri Resolve(string regionalHost) => new($"https://{regionalHost}.api.riotgames.com");
}
