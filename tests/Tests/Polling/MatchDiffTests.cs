using LolMatchAlert.Core.Polling;

namespace LolMatchAlert.Tests.Polling;

public sealed class MatchDiffTests
{
    [Fact]
    public void Tom_liste_giver_ingen_nye_og_bevarer_baseline()
    {
        var result = MatchDiff.Compute("m3", []);

        Assert.Empty(result.NewMatchIdsOldestFirst);
        Assert.Equal("m3", result.NewLastSeenMatchId);
        Assert.False(result.IsFirstSight);
    }

    [Fact]
    public void Foerste_syn_poster_ikke_historik_men_saetter_baseline()
    {
        // recent er nyeste-først.
        var result = MatchDiff.Compute(null, ["m5", "m4", "m3"]);

        Assert.Empty(result.NewMatchIdsOldestFirst);
        Assert.Equal("m5", result.NewLastSeenMatchId);
        Assert.True(result.IsFirstSight);
    }

    [Fact]
    public void Ingen_nye_kampe_naar_nyeste_er_sidst_sete()
    {
        var result = MatchDiff.Compute("m5", ["m5", "m4", "m3"]);

        Assert.Empty(result.NewMatchIdsOldestFirst);
        Assert.Equal("m5", result.NewLastSeenMatchId);
    }

    [Fact]
    public void Nye_kampe_returneres_aeldste_foerst()
    {
        // lastSeen = m3; nyere er m5 og m4. Skal postes kronologisk: m4 så m5.
        var result = MatchDiff.Compute("m3", ["m5", "m4", "m3", "m2", "m1"]);

        Assert.Equal(["m4", "m5"], result.NewMatchIdsOldestFirst);
        Assert.Equal("m5", result.NewLastSeenMatchId);
        Assert.False(result.IsFirstSight);
    }

    [Fact]
    public void Een_ny_kamp()
    {
        var result = MatchDiff.Compute("m4", ["m5", "m4", "m3"]);

        Assert.Equal(["m5"], result.NewMatchIdsOldestFirst);
        Assert.Equal("m5", result.NewLastSeenMatchId);
    }

    [Fact]
    public void Sidst_sete_uden_for_vinduet_poster_hele_vinduet()
    {
        // lastSeen 'gammel' findes ikke i de seneste 3 -> spilleren har spillet
        // mere end vinduet siden sidst. Post alt vi kan se, ældste først.
        var result = MatchDiff.Compute("m1", ["m5", "m4", "m3"]);

        Assert.Equal(["m3", "m4", "m5"], result.NewMatchIdsOldestFirst);
        Assert.Equal("m5", result.NewLastSeenMatchId);
    }
}
