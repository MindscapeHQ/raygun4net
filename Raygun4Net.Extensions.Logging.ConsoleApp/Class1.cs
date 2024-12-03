using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mindscape.Raygun4Net.Extensions.Logging;

//using CustomLogging;

namespace Raygun4Net.Extensions.Logging.ConsoleApp;

public class Program
{
  public static void Main(string[] args)
  {
    var serviceCollection = new ServiceCollection();
    
    // Add logging
    serviceCollection.AddLogging(builder =>
    {
      // Add console logging to see logs in console too
      builder.AddConsole();

      builder.AddRaygunLogger(options: settings =>
      {
        settings.ApiKey = "zqpKCLNE8SXj7aBfjZv98w";
      });
    });

    // Register WeatherService
    serviceCollection.AddTransient<WeatherService>();

    var serviceProvider = serviceCollection.BuildServiceProvider();

    // Get an instance of WeatherService which will use our logger
    var weatherService = serviceProvider.GetRequiredService<WeatherService>();
    weatherService.SimulateWeatherOperations();

    // Get a logger directly
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Direct logging from Program.cs");

    // Simulate an error
    try
    {
      throw new Exception("Simulated error!");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "An error occurred during application execution");
    }

    Console.WriteLine("Logs have been written to the database. Press any key to exit.");
    Console.ReadKey();
  }
}

public class WeatherService
{
  private readonly ILogger<WeatherService> _logger;

  public WeatherService(ILogger<WeatherService> logger)
  {
    _logger = logger;
  }

  public void SimulateWeatherOperations()
  {
    _logger.LogInformation("Starting weather simulation at {Time}", DateTime.Now);

    var temperature = Random.Shared.Next(-10, 35);
    _logger.LogDebug("Generated random temperature: {Temperature}°C", temperature);

    if (temperature > 30)
    {
      _logger.LogWarning("High temperature detected: {Temperature}°C", temperature);
    }
    else if (temperature < 0)
    {
      _logger.LogWarning("Freezing temperature detected: {Temperature}°C", temperature);
    }
    else
    {
      _logger.LogInformation("Normal temperature: {Temperature}°C", temperature);
    }

    // Simulate some weather processing
    for (int i = 1; i <= 3; i++)
    {
      _logger.LogInformation("Processing weather data - Step {Step}", i);
      Thread.Sleep(500); // Simulate some work
    }

    _logger.LogInformation("Weather simulation completed");
  }
}