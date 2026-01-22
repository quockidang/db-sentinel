using DbSentinel.Collector;
using DbSentinel.Parser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ISlowLogParser, JsonSlowLogParser>();
builder.Services.AddHostedService<Worker>();

// The IConfiguration and ILogger are automatically registered by CreateApplicationBuilder.
// The DI container will automatically resolve the ILogger<JsonSlowLogParser> dependency.

var host = builder.Build();
host.Run();
