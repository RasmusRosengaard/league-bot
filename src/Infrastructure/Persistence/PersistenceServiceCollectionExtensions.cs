using LolMatchAlert.Core.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LolMatchAlert.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public const string ConnectionStringName = "Default";

    /// <summary>Registrerer BotDbContext (PostgreSQL) og repositories.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Mangler connection string 'ConnectionStrings:{ConnectionStringName}'. Sæt den via konfiguration/miljøvariabel.");

        services.AddDbContext<BotDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(BotDbContext).Assembly.FullName)));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ILastSeenMatchRepository, LastSeenMatchRepository>();

        return services;
    }
}
