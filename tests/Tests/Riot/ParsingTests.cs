using LolMatchAlert.Core.Riot;

namespace LolMatchAlert.Tests.Riot;

public sealed class ParsingTests
{
    [Theory]
    [InlineData("Faker#KR1", "Faker", "KR1")]
    [InlineData("Here Comes The Sun#2019", "Here Comes The Sun", "2019")]
    [InlineData("  Name#TAG  ", "Name", "TAG")]
    [InlineData("a#b#c", "a#b", "c")] // splitter på sidste '#'
    public void RiotId_parses(string input, string expectedName, string expectedTag)
    {
        Assert.True(RiotId.TryParse(input, out var id));
        Assert.Equal(expectedName, id!.GameName);
        Assert.Equal(expectedTag, id.TagLine);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NoTag")]
    [InlineData("#TagOnly")]
    [InlineData("NameOnly#")]
    public void RiotId_afviser_ugyldigt(string? input)
    {
        Assert.False(RiotId.TryParse(input, out _));
    }

    [Theory]
    [InlineData("euw1", "euw1", "europe")]
    [InlineData("EUW", "euw1", "europe")]
    [InlineData("na", "na1", "americas")]
    [InlineData("kr", "kr", "asia")]
    [InlineData("oce", "oc1", "sea")]
    public void Region_parses_og_router(string input, string expectedPlatform, string expectedHost)
    {
        Assert.True(Regions.TryParse(input, out var region));
        Assert.Equal(expectedPlatform, region!.Platform);
        Assert.Equal(expectedHost, region.RegionalHost);
    }

    [Theory]
    [InlineData("atlantis")]
    [InlineData("")]
    [InlineData(null)]
    public void Region_afviser_ukendt(string? input)
    {
        Assert.False(Regions.TryParse(input, out _));
    }
}
