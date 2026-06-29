using System.Globalization;
using System.Net;
using System.Text.Json;
using LolMatchAlert.Core.Riot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LolMatchAlert.Infrastructure.Riot;

/// <summary>
/// HttpClient-baseret implementation af <see cref="IRiotClient"/>.
/// Resilience (retry/timeout/circuit-breaker + Retry-After) konfigureres på den
/// typed client i DI — se RiotClientServiceCollectionExtensions.
/// </summary>
public sealed class RiotClient(
    HttpClient httpClient,
    IRiotHostResolver hostResolver,
    IOptions<RiotOptions> options,
    ILogger<RiotClient> logger) : IRiotClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RiotOptions _options = options.Value;

    public async Task<RiotAccount?> GetAccountByRiotIdAsync(RiotId riotId, Region region, CancellationToken cancellationToken)
    {
        var path = $"/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(riotId.GameName)}/{Uri.EscapeDataString(riotId.TagLine)}";
        var dto = await GetAsync<AccountDto>(region.RegionalHost, path, notFoundAsNull: true, cancellationToken);
        if (dto is null)
        {
            return null;
        }

        // account-v1 ekkoer normalt gameName/tagLine tilbage; fald tilbage på input hvis ikke.
        return new RiotAccount(dto.Puuid, dto.GameName ?? riotId.GameName, dto.TagLine ?? riotId.TagLine);
    }

    public async Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, Region region, int count, CancellationToken cancellationToken)
    {
        var clamped = Math.Clamp(count, 1, 100);
        var path = $"/lol/match/v5/matches/by-puuid/{Uri.EscapeDataString(puuid)}/ids?start=0&count={clamped}";
        var ids = await GetAsync<List<string>>(region.RegionalHost, path, notFoundAsNull: true, cancellationToken);
        return ids ?? [];
    }

    public async Task<MatchDetail?> GetMatchAsync(string matchId, Region region, CancellationToken cancellationToken)
    {
        var path = $"/lol/match/v5/matches/{Uri.EscapeDataString(matchId)}";
        var dto = await GetAsync<MatchDto>(region.RegionalHost, path, notFoundAsNull: true, cancellationToken);
        return dto is null ? null : MapMatch(dto);
    }

    private static MatchDetail MapMatch(MatchDto dto)
    {
        // gameDuration var i millisekunder før patch 11.20; efter i sekunder.
        // Tilstedeværelsen af gameEndTimestamp markerer det nye format (sekunder).
        var duration = dto.Info.GameEndTimestamp.HasValue
            ? TimeSpan.FromSeconds(dto.Info.GameDuration)
            : TimeSpan.FromMilliseconds(dto.Info.GameDuration);

        var participants = dto.Info.Participants
            .Select(p => new MatchParticipant(
                p.Puuid,
                p.ChampionName ?? "Unknown",
                p.Win,
                p.Kills,
                p.Deaths,
                p.Assists,
                p.RiotIdGameName,
                p.RiotIdTagline))
            .ToArray();

        return new MatchDetail(
            dto.Metadata.MatchId,
            dto.Info.QueueId,
            dto.Info.GameMode ?? "UNKNOWN",
            duration,
            participants);
    }

    private async Task<T?> GetAsync<T>(string regionalHost, string path, bool notFoundAsNull, CancellationToken cancellationToken)
    {
        var url = new Uri(hostResolver.Resolve(regionalHost), path);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Riot-Token", _options.ApiKey);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (notFoundAsNull && response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        EnsureSuccess(response, regionalHost, path);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private void EnsureSuccess(HttpResponseMessage response, string regionalHost, string path)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var code = (int)response.StatusCode;
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
                logger.LogError(
                    "Riot afviste kald til {Host}{Path} med {Status}. Er API-nøglen gyldig? (development-nøgler udløber hver 24. time)",
                    regionalHost, path, code);
                throw new RiotAuthException(
                    $"Riot afviste kaldet ({code}). Tjek at API-nøglen er sat og ikke er udløbet.", code);

            case HttpStatusCode.TooManyRequests:
                var retryAfter = ReadRetryAfter(response);
                logger.LogWarning(
                    "Riot rate limit (429) på {Host}{Path}. Retry-After: {RetryAfter}.",
                    regionalHost, path, retryAfter);
                throw new RiotRateLimitException(
                    "Riot rate limit ramt (429) og opgivet efter retries.", retryAfter);

            default:
                logger.LogError("Riot-kald til {Host}{Path} fejlede med {Status}.", regionalHost, path, code);
                throw new RiotApiException($"Riot-kald fejlede med status {code}.", code);
        }
    }

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (response.Headers.TryGetValues("Retry-After", out var values)
            && int.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }
}
