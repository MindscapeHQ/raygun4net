# Mindscape.Raygun4Net.Extensions.Logging

This package provides a logging provider for the Raygun service. It allows you to send log messages to Raygun, 
where they can be viewed and managed in the Raygun dashboard.

## Installation

Install the **Mindscape.Raygun4Net.Extensions.Logging** NuGet package into your project. You can either use the below dotnet CLI command, or the NuGet management GUI in the IDE you use.

```csharp
dotnet add package Mindscape.Raygun4Net.Extensions.Logging
```

You will need to install the **Mindscape.Raygun4Net** package as well, if you haven't already.

```csharp
// If you're using it in an ASP.NET Core application:
dotnet add package Mindscape.Raygun4Net.AspNetCore

// If you're using it in a .NET Core service application:
dotnet add package Mindscape.Raygun4Net.NetCore
```

## Usage

Add the Raygun provider to the logging configuration in your `Program.cs` or `Startup.cs` file.

### ASP.NET Core Application

```csharp
using Mindscape.Raygun4Net.AspNetCore;
using Mindscape.Raygun4Net.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Registers the Raygun Client for AspNetCore
builder.Services.AddRaygun(settings =>
{
  settings.ApiKey = "*your api key*";
});
   
// (Optional) Registers the Raygun User Provider
builder.Services.AddRaygunUserProvider();

// Registers the Raygun Logger for use in MS Logger
builder.Logging.AddRaygunLogger();

var app = builder.Build();
```

### .NET Core Service Application

```csharp
using Mindscape.Raygun4Net.Extensions.Logging;
using Mindscape.Raygun4Net.NetCore;

var builder = Host.CreateApplicationBuilder(args);

// Registers the Raygun Client for NetCore
builder.Services.AddRaygun(options =>
{
  options.ApiKey = "*your api key*";
});

// Registers the Raygun Logger for use in MS Logger
builder.Logging.AddRaygunLogger();

var host = builder.Build();
```

## Configuration

When registering the Raygun provider, you can configure it with the following options:

* MinimumLogLevel: The minimum log level for messages to be sent to Raygun. Defaults to `LogLevel.Error`.
* OnlyLogExceptions: If false, logs without exceptions will be sent to Raygun. Defaults to `true`.

These can be set in code:

```csharp
builder.Logging.AddRaygunLogger(options: options =>
{
  options.MinimumLogLevel = LogLevel.Information;
  options.OnlyLogExceptions = false;
});
```

Or in the `appsettings.json` file:

```json
{
  "RaygunSettings": {
    "MinimumLogLevel": "Information",
    "OnlyLogExceptions": false
  }
}
```

## Notes

The category/contextSource set as a tag in Raygun.

When logs are sent without an exception, a Custom Data property is added to the Raygun message with the 
key `NullException` with the value `Logged without exception` to identify Raygun Logs that have no exception.
