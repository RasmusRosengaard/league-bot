using LolMatchAlert.Bot.Discord;
using LolMatchAlert.Infrastructure.Persistence;
using LolMatchAlert.Infrastructure.Riot;

var builder = Host.CreateApplicationBuilder(args);

// Struktureret logging via det indbyggede ILogger-framework.
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Riot-klient (resilience), persistering (Postgres + repositories), Discord-bot.
builder.Services.AddRiotClient(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddDiscordBot(builder.Configuration);

var host = builder.Build();
host.Run();
