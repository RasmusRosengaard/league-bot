using LolMatchAlert.Core.Polling;
using LolMatchAlert.Core.Riot;

namespace LolMatchAlert.Tests.Polling;

public sealed class MatchAlertFormattingTests
{
    private static MatchAlert Alert(bool win = false, int k = 7, int d = 7, int a = 13, int queue = 420) => new(
        MatchId: "EUW1_1",
        RiotId: new RiotId("HereComesTheSun", "2019"),
        Region: "euw1",
        ChampionName: "Sion",
        Win: win,
        Kills: k,
        Deaths: d,
        Assists: a,
        QueueId: queue,
        GameMode: "CLASSIC",
        GameDuration: TimeSpan.FromSeconds(1719));

    [Fact]
    public void Kda_og_ratio_beregnes()
    {
        var alert = Alert(k: 7, d: 7, a: 13);
        Assert.Equal("7/7/13", alert.Kda);
        Assert.Equal("7/7/13 (2.86 KDA)", MatchAlertFormatter.KdaWithRatio(alert));
    }

    [Fact]
    public void Ratio_haandterer_nul_deaths()
    {
        var alert = Alert(k: 5, d: 0, a: 5);
        Assert.Equal(10.0, alert.KdaRatio);
    }

    [Fact]
    public void Duration_formateres_som_mm_ss()
    {
        Assert.Equal("28:39", Alert().DurationText); // 1719s = 28:39
    }

    [Fact]
    public void QueueName_oversaettes()
    {
        Assert.Equal("Ranked Solo/Duo", Alert(queue: 420).QueueName);
        Assert.Equal("ARAM", Alert(queue: 450).QueueName);
    }

    [Fact]
    public void QueueName_falder_tilbage_paa_gamemode_for_ukendt_queue()
    {
        Assert.Equal("CLASSIC", QueueNames.Get(99999, "CLASSIC"));
        Assert.Equal("Queue 99999", QueueNames.Get(99999));
    }

    [Fact]
    public void ShortLine_og_Title()
    {
        var loss = Alert(win: false);
        Assert.Equal("HereComesTheSun#2019 spillede Sion og tabte — 7/7/13 i Ranked Solo/Duo", MatchAlertFormatter.ShortLine(loss));
        Assert.Equal("Nederlag som Sion", MatchAlertFormatter.Title(loss));

        var winAlert = Alert(win: true);
        Assert.Equal("Sejr som Sion", MatchAlertFormatter.Title(winAlert));
    }
}
