using LolMatchAlert.Core.Riot;
using LolMatchAlert.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace LolMatchAlert.Tests.Riot;

/// <summary>
/// Integrationstests for RiotClient mod en WireMock.Net-server, så de er
/// deterministiske og uafhængige af det rigtige Riot-API.
/// </summary>
public sealed class RiotClientIntegrationTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly Region _region;

    public RiotClientIntegrationTests()
    {
        Assert.True(Regions.TryParse("euw1", out var region));
        _region = region;
    }

    private IRiotClient Client(out ServiceProvider provider, string apiKey = "test-key")
    {
        provider = RiotTestHost.Build(_server.Url!, apiKey);
        return provider.GetRequiredService<IRiotClient>();
    }

    [Fact]
    public async Task GetAccount_returnerer_konto_og_sender_api_noegle()
    {
        _server
            .Given(Request.Create()
                .WithPath("/riot/account/v1/accounts/by-riot-id/HereComesTheSun/2019")
                .WithHeader("X-Riot-Token", "secret-123")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(TestFixtures.Account));

        var client = Client(out var provider, apiKey: "secret-123");
        await using var _ = provider;

        var account = await client.GetAccountByRiotIdAsync(new RiotId("HereComesTheSun", "2019"), _region, CancellationToken.None);

        Assert.NotNull(account);
        Assert.Equal(TestFixtures.Puuid, account!.Puuid);
        Assert.Equal("HereComesTheSun", account.GameName);
        Assert.Equal("2019", account.TagLine);
    }

    [Fact]
    public async Task GetAccount_returnerer_null_ved_404()
    {
        _server
            .Given(Request.Create().WithPath("/riot/account/v1/accounts/by-riot-id/Nobody/0000").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404).WithBody("{\"status\":{\"status_code\":404}}"));

        var client = Client(out var provider);
        await using var _ = provider;

        var account = await client.GetAccountByRiotIdAsync(new RiotId("Nobody", "0000"), _region, CancellationToken.None);

        Assert.Null(account);
    }

    [Fact]
    public async Task GetAccount_kaster_RiotAuthException_ved_403()
    {
        _server
            .Given(Request.Create().WithPath("/riot/account/v1/accounts/by-riot-id/X/Y").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(403));

        var client = Client(out var provider);
        await using var _ = provider;

        var ex = await Assert.ThrowsAsync<RiotAuthException>(() =>
            client.GetAccountByRiotIdAsync(new RiotId("X", "Y"), _region, CancellationToken.None));
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task GetRecentMatchIds_returnerer_id_liste()
    {
        _server
            .Given(Request.Create()
                .WithPath($"/lol/match/v5/matches/by-puuid/{TestFixtures.Puuid}/ids")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(TestFixtures.MatchIds));

        var client = Client(out var provider);
        await using var _ = provider;

        var ids = await client.GetRecentMatchIdsAsync(TestFixtures.Puuid, _region, 5, CancellationToken.None);

        Assert.Equal(5, ids.Count);
        Assert.Equal(TestFixtures.MatchId, ids[0]);
    }

    [Fact]
    public async Task GetMatch_parser_detaljer_og_finder_deltager()
    {
        _server
            .Given(Request.Create().WithPath($"/lol/match/v5/matches/{TestFixtures.MatchId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(TestFixtures.Match));

        var client = Client(out var provider);
        await using var _ = provider;

        var match = await client.GetMatchAsync(TestFixtures.MatchId, _region, CancellationToken.None);

        Assert.NotNull(match);
        Assert.Equal(TestFixtures.MatchId, match!.MatchId);
        Assert.Equal(420, match.QueueId);
        Assert.Equal("CLASSIC", match.GameMode);
        Assert.Equal(TimeSpan.FromSeconds(1719), match.GameDuration);
        Assert.Equal(10, match.Participants.Count);

        var participant = match.FindParticipant(TestFixtures.Puuid);
        Assert.NotNull(participant);
        Assert.Equal("Sion", participant!.ChampionName);
        Assert.False(participant.Win);
        Assert.Equal("7/7/13", participant.Kda);
    }

    [Fact]
    public async Task GetMatch_kaster_RiotRateLimitException_ved_vedvarende_429()
    {
        _server
            .Given(Request.Create().WithPath($"/lol/match/v5/matches/{TestFixtures.MatchId}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "1"));

        var client = Client(out var provider);
        await using var _ = provider;

        var ex = await Assert.ThrowsAsync<RiotRateLimitException>(() =>
            client.GetMatchAsync(TestFixtures.MatchId, _region, CancellationToken.None));
        Assert.Equal(429, ex.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(1), ex.RetryAfter);
    }

    public void Dispose() => _server.Stop();
}
