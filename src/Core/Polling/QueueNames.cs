namespace LolMatchAlert.Core.Polling;

/// <summary>Oversætter match-v5 queueId til et menneskeligt navn. Falder tilbage på gameMode.</summary>
public static class QueueNames
{
    private static readonly Dictionary<int, string> Names = new()
    {
        [400] = "Normal (Draft)",
        [420] = "Ranked Solo/Duo",
        [430] = "Normal (Blind)",
        [440] = "Ranked Flex",
        [450] = "ARAM",
        [490] = "Quickplay",
        [700] = "Clash",
        [720] = "ARAM Clash",
        [830] = "Co-op vs. AI (Intro)",
        [840] = "Co-op vs. AI (Beginner)",
        [850] = "Co-op vs. AI (Intermediate)",
        [900] = "ARURF",
        [1020] = "One for All",
        [1300] = "Nexus Blitz",
        [1700] = "Arena",
        [1900] = "URF",
    };

    public static string Get(int queueId, string? gameModeFallback = null)
    {
        if (Names.TryGetValue(queueId, out var name))
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(gameModeFallback))
        {
            return gameModeFallback;
        }

        return $"Queue {queueId}";
    }
}
