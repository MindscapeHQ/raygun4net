using Microsoft.Extensions.Logging;

namespace Mindscape.Raygun4Net.Extensions.Logging;

public class RaygunLoggerSettings
{
  /// <summary>
  /// Specifies the minimum log level that will be processed by the Raygun logger.
  /// </summary>
  /// <remarks>
  /// LogLevel determines the severity of logs that are allowed to be emitted by the logger.
  /// Messages with a severity level below this setting will be ignored.
  /// The default value is LogLevel.Error.
  /// </remarks>
  public LogLevel MinimumLogLevel { get; set; } = LogLevel.Error;

  /// <summary>
  /// Determines whether only exceptions should be logged to Raygun.
  /// </summary>
  /// <remarks>
  /// When set to true, only exceptions are sent to Raygun. Other log messages are ignored by the logger.
  /// Default value is true.
  /// </remarks>
  public bool OnlyLogExceptions { get; set; } = true;
}