using LolMatchAlert.Bot.Discord;
using LolMatchAlert.Core.Polling;
using LolMatchAlert.Core.Riot;

namespace LolMatchAlert.Tests.Polling;

public sealed class EmbedAndIconTests
{
    private static MatchAlert Alert(bool win) => new(
        "EUW1_7819217541", new RiotId("HereComesTheSun", "2019"), "euw1",
        "Sion", win, 7, 7, 13, 420, "CLASSIC", TimeSpan.FromSeconds(1719));

    [Fact]
    public void ChampionIconUrl_bygges_korrekt()
    {
        var url = DataDragon.ChampionIconUrl("15.13.1", "Sion");
        Assert.Equal("https://ddragon.leagueoflegends.com/cdn/15.13.1/img/champion/Sion.png", url);
    }

    [Fact]
    public void ChampionIconUrl_falder_tilbage_paa_default_version()
    {
        var url = DataDragon.ChampionIconUrl("", "Sion");
        Assert.Contains(DataDragon.DefaultVersion, url, StringComparison.Ordinal);
    }

    [Fact]
    public void Embed_for_sejr_er_groen_med_rette_felter()
    {
        var embed = MatchEmbedFactory.Build(Alert(win: true), "15.13.1");

        Assert.Equal("Sejr som Sion", embed.Title);
        Assert.Equal(0x2ECC71u, embed.Color!.Value.RawValue);
        Assert.Contains(embed.Fields, f => f.Name == "KDA" && f.Value.Contains("7/7/13", StringComparison.Ordinal));
        Assert.Contains(embed.Fields, f => f.Name == "Kø" && f.Value == "Ranked Solo/Duo");
        Assert.Contains("Sion.png", embed.Thumbnail!.Value.Url, StringComparison.Ordinal);
    }

    [Fact]
    public void Embed_for_nederlag_er_roed()
    {
        var embed = MatchEmbedFactory.Build(Alert(win: false), "15.13.1");
        Assert.Equal(0xE74C3Cu, embed.Color!.Value.RawValue);
        Assert.Equal("Nederlag som Sion", embed.Title);
    }
}
