namespace Raygun4Net.MSLogger.AspNetCore.Tests.Models;

public class ErrorViewModel
{
  public string? RequestId { get; set; }

  public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}