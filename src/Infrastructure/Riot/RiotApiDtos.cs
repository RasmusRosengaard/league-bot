using System.Text.Json.Serialization;

namespace LolMatchAlert.Infrastructure.Riot;

// Interne DTO'er der matcher Riots JSON 1:1. Mappes til Core-domænemodeller i RiotClient,
// så resten af løsningen ikke kender til det rå skema.

internal sealed record AccountDto(
    [property: JsonPropertyName("puuid")] string Puuid,
    [property: JsonPropertyName("gameName")] string? GameName,
    [property: JsonPropertyName("tagLine")] string? TagLine);

internal sealed record MatchDto(
    [property: JsonPropertyName("metadata")] MatchMetadataDto Metadata,
    [property: JsonPropertyName("info")] MatchInfoDto Info);

internal sealed record MatchMetadataDto(
    [property: JsonPropertyName("matchId")] string MatchId);

internal sealed record MatchInfoDto(
    [property: JsonPropertyName("queueId")] int QueueId,
    [property: JsonPropertyName("gameMode")] string? GameMode,
    [property: JsonPropertyName("gameDuration")] long GameDuration,
    [property: JsonPropertyName("gameEndTimestamp")] long? GameEndTimestamp,
    [property: JsonPropertyName("participants")] IReadOnlyList<ParticipantDto> Participants);

internal sealed record ParticipantDto(
    [property: JsonPropertyName("puuid")] string Puuid,
    [property: JsonPropertyName("championName")] string? ChampionName,
    [property: JsonPropertyName("win")] bool Win,
    [property: JsonPropertyName("kills")] int Kills,
    [property: JsonPropertyName("deaths")] int Deaths,
    [property: JsonPropertyName("assists")] int Assists,
    [property: JsonPropertyName("riotIdGameName")] string? RiotIdGameName,
    [property: JsonPropertyName("riotIdTagline")] string? RiotIdTagline);
