# Mindscape.Raygun4Net.Extensions.Logging

This package provides a logging provider for the Raygun service. It allows you to send log messages to Raygun, 
where they can be viewed and managed in the Raygun dashboard.

## Installation

Install the **Mindscape.Raygun4Net.Extensions.Logging** NuGet package into your project. You can either use the below dotnet CLI command, or the NuGet management GUI in the IDE you use.

```
dotnet add package Mindscape.Raygun4Net.Extensions.Logging
```

## Usage

Add the Raygun provider to the logging configuration in your `Program.cs` or `Startup.cs` file.

```csharp
using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net.Extensions.Logging;

public class Program
{
    public static void Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddRaygun("paste_your_api_key_here");
        });

        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("Hello, Raygun!");
    }
}
```