using LolMatchAlert.Core.Polling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LolMatchAlert.Bot.Polling;

public static class PollingServiceCollectionExtensions
{
    public static IServiceCollection AddPolling(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PollingOptions>()
            .Bind(configuration.GetSection(PollingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Coordinator afhænger af scoped last-seen-repo -> scoped.
        services.AddScoped<MatchPollingCoordinator>();
        services.AddHostedService<MatchPollingService>();

        return services;
    }
}
