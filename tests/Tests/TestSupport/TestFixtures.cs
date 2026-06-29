namespace LolMatchAlert.Tests.TestSupport;

/// <summary>Indlæser gemte JSON-eksempelsvar fra Fixtures-mappen (kopieret til output).</summary>
internal static class TestFixtures
{
    private static readonly string FixturesDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static string Read(string fileName) =>
        File.ReadAllText(Path.Combine(FixturesDir, fileName));

    // Navngivne fixtures brugt på tværs af tests.
    public static string Account => Read("account_HereComesTheSun.json");
    public static string MatchIds => Read("match_ids_HereComesTheSun.json");
    public static string Match => Read("match_EUW1_7819217541.json");

    public const string Puuid = "WSn_DBnREO0wwTeygbAZRN5xPpO9SsDGWQG8HY7f-T1SXDX3Sm730oDu2TcHQLW8WS4Lm7mm-THMZw";
    public const string MatchId = "EUW1_7819217541";
}
