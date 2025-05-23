using Mindscape.Raygun4Net.Extensions.Logging;
using Mindscape.Raygun4Net.NetCore;
using Raygun4Net.MSLogger.Service.Tests;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();

// Registers the Raygun Client for NetCore
builder.Services.AddRaygun(options =>
{
  options.ApiKey = "*your_api_key*";
});

// (Optional) Add Raygun User Provider - no default implementation provided in non ASP.NET projects
//builder.Services.AddRaygunUserProvider<...>()

// Registers the Raygun Logger for use in MS Logger
builder.Logging.AddRaygunLogger();

var host = builder.Build();

host.Run();