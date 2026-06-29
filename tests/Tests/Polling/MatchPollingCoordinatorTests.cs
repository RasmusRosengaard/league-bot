using LolMatchAlert.Core.Polling;
using LolMatchAlert.Core.Riot;
using LolMatchAlert.Core.Subscriptions;
using LolMatchAlert.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace LolMatchAlert.Tests.Polling;

public sealed class MatchPollingCoordinatorTests
{
    private const string Puuid = "puuid-HereComesTheSun";

    private static AccountToPoll Account()
    {
        Assert.True(Regions.TryParse("euw1", out var region));
        return new AccountToPoll(Puuid, "HereComesTheSun", "2019", region);
    }

    private static MatchDetail Match(string matchId, bool win, string champ = "Sion") => new(
        matchId, QueueId: 420, GameMode: "CLASSIC", GameDuration: TimeSpan.FromSeconds(1200),
        Participants:
        [
            new MatchParticipant(Puuid, champ, win, 5, 3, 7, "HereComesTheSun", "2019"),
            new MatchParticipant("someone-else", "Lux", !win, 1, 1, 1, "Other", "EUW"),
        ]);

    private static MatchPollingCoordinator Build(FakeRiotClient riot, FakeLastSeenMatchRepository repo) =>
        new(riot, repo, NullLogger<MatchPollingCoordinator>.Instance);

    [Fact]
    public async Task Foerste_poll_giver_ingen_alerts_men_saetter_baseline()
    {
        var riot = new FakeRiotClient();
        riot.MatchIdsByPuuid[Puuid] = ["m3", "m2", "m1"];
        riot.MatchesById["m3"] = Match("m3", win: true);
        var repo = new FakeLastSeenMatchRepository();

        var alerts = await Build(riot, repo).PollAccountAsync(Account(), 5, CancellationToken.None);

        Assert.Empty(alerts);
        Assert.Equal("m3", repo.Store[Puuid]);
        Assert.Empty(riot.RequestedMatchIds); // ingen detalje-kald ved første-syn
    }

    [Fact]
    public async Task Nye_kampe_giver_alerts_i_kronologisk_raekkefoelge()
    {
        var riot = new FakeRiotClient();
        riot.MatchIdsByPuuid[Puuid] = ["m3", "m2", "m1"];
        riot.MatchesById["m2"] = Match("m2", win: false);
        riot.MatchesById["m3"] = Match("m3", win: true);
        var repo = new FakeLastSeenMatchRepository();
        repo.Store[Puuid] = "m1"; // baseline: kun m2 og m3 er nye

        var alerts = await Build(riot, repo).PollAccountAsync(Account(), 5, CancellationToken.None);

        Assert.Equal(2, alerts.Count);
        Assert.Equal("m2", alerts[0].MatchId);
        Assert.Equal("m3", alerts[1].MatchId);
        Assert.False(alerts[0].Win);
        Assert.True(alerts[1].Win);
        Assert.Equal("m3", repo.Store[Puuid]); // baseline opdateret til nyeste
    }

    [Fact]
    public async Task Anden_poll_uden_nye_kampe_er_idempotent()
    {
        var riot = new FakeRiotClient();
        riot.MatchIdsByPuuid[Puuid] = ["m3", "m2", "m1"];
        riot.MatchesById["m3"] = Match("m3", win: true);
        var repo = new FakeLastSeenMatchRepository();
        repo.Store[Puuid] = "m3";

        var alerts = await Build(riot, repo).PollAccountAsync(Account(), 5, CancellationToken.None);

        Assert.Empty(alerts);
        Assert.Equal("m3", repo.Store[Puuid]);
    }

    [Fact]
    public async Task Match_der_ikke_kan_hentes_springes_over()
    {
        var riot = new FakeRiotClient();
        riot.MatchIdsByPuuid[Puuid] = ["m3", "m2"];
        // m3 er ny (baseline=m2), men findes ikke i MatchesById -> GetMatchAsync giver null.
        var repo = new FakeLastSeenMatchRepository();
        repo.Store[Puuid] = "m2";

        var alerts = await Build(riot, repo).PollAccountAsync(Account(), 5, CancellationToken.None);

        Assert.Empty(alerts); // m3 kunne ikke hentes
        Assert.Equal("m3", repo.Store[Puuid]); // baseline rykkes stadig frem
    }
}
