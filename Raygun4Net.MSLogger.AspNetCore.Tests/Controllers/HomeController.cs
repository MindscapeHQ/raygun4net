using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Raygun4Net.MSLogger.AspNetCore.Tests.Models;

namespace Raygun4Net.MSLogger.AspNetCore.Tests.Controllers;

public class HomeController : Controller
{
  private readonly ILogger<HomeController> _logger;

  public HomeController(ILogger<HomeController> logger)
  {
    _logger = logger;
  }

  public IActionResult Index()
  {
    throw new Exception("Banana was not yellow.");
  }

  [HttpGet("test-2")]
  public IActionResult ScopeNotCaptured()
  {
    // Because we throw and the capture happens outside the scope, the scope data is not captured.
    using (_logger.BeginScope("Banana"))
    using (_logger.BeginScope("User {Thing}", "Fred"))
    using (_logger.BeginScope("Age {Age}", 30))
    {
      throw new Exception("A scoped exception");
    }
  }

  [HttpGet("test-3")]
  public IActionResult ScopeCaptured()
  {
    // Because we catch the exception and the capture happens inside the scope, the scope data is captured.
    using (_logger.BeginScope("Banana"))
    using (_logger.BeginScope("User {Thing}", "Fred"))
    using (_logger.BeginScope("Age {Age}", 30))
    {
      try
      {
        throw new Exception("A scoped exception");
      }
      catch (Exception e)
      {
        _logger.LogError(e, "An error occurred");
        return Problem(detail: "An error occurred", statusCode: 500);
      }
    }
  }

  [HttpGet("test-4")]
  public IActionResult Error()
  {
    _logger.Log(LogLevel.Information, "An error occurred, don't know what tho...");
    
    return Content("Hello world", "text/plain");
  }
}