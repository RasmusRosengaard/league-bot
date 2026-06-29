using LolMatchAlert.Core.Riot;
using LolMatchAlert.Infrastructure.Riot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LolMatchAlert.Tests.TestSupport;

/// <summary>Resolver der peger alle regionale kald mod én fast (WireMock-)base-URL.</summary>
internal sealed class FixedHostResolver(Uri baseUri) : IRiotHostResolver
{
    public Uri Resolve(string regionalHost) => baseUri;
}

/// <summary>
/// Bygger en rigtig DI-container med produktionens <see cref="RiotClient"/> + resilience,
/// men med host-resolveren peget mod en test-server.
/// </summary>
internal static class RiotTestHost
{
    public static ServiceProvider Build(string baseUrl, string apiKey = "test-key")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{RiotOptions.SectionName}:ApiKey"] = apiKey,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRiotClient(config);

        // Overskriv default-resolveren (registreret med TryAdd) så kald rammer WireMock.
        services.AddSingleton<IRiotHostResolver>(new FixedHostResolver(new Uri(baseUrl)));

        return services.BuildServiceProvider();
    }
}
