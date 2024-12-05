namespace Raygun4Net.MSLogger.Service.Tests;

public class Worker : BackgroundService
{
  private readonly ILogger<Worker> _logger;

  public Worker(ILogger<Worker> logger)
  {
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.Log(LogLevel.Information, "Worker running at: {Time}", DateTimeOffset.Now);

    try
    {
      throw new Exception("Test exception");
    }
    catch (Exception e)
    {
      _logger.LogError(e, "An error occurred");
      
      await Task.Delay(1000, stoppingToken);
      
      throw;
    }
  }
}