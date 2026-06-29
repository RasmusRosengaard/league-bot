using System.Net;
using LolMatchAlert.Core.Riot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace LolMatchAlert.Infrastructure.Riot;

public static class RiotClientServiceCollectionExtensions
{
    /// <summary>
    /// Registrerer <see cref="IRiotClient"/> som en typed HttpClient med
    /// resilience: retry (honorerer Retry-After ved 429), attempt-timeout og
    /// circuit-breaker.
    /// </summary>
    public static IServiceCollection AddRiotClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IRiotHostResolver, RiotHostResolver>();

        services.AddOptions<RiotOptions>()
            .Bind(configuration.GetSection(RiotOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IRiotClient, RiotClient>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LolMatchAlert/1.0 (+https://github.com)");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddStandardResilienceHandler(ConfigureResilience);

        return services;
    }

    internal static void ConfigureResilience(HttpStandardResilienceOptions options)
    {
        // Retry: håndtér transiente fejl OG 429, og brug Retry-After-headeren
        // som vente-tid når Riot oplyser den.
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.ShouldRetryAfterHeader = true;
        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            HttpClientResiliencePredicates.IsTransient(args.Outcome)
            || args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests);

        // Timeouts: pr. forsøg + samlet budget på tværs af retries.
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);

        // Circuit breaker: SamplingDuration skal være >= 2 * AttemptTimeout.
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    }
}
