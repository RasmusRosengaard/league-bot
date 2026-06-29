using LolMatchAlert.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Struktureret logging via det indbyggede ILogger-framework.
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Services.AddHostedService<HeartbeatService>();

var host = builder.Build();
host.Run();
