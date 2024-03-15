using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net;

public abstract class RaygunSettingsBase
{
  private const string DefaultApiEndPoint = "https://api.raygun.com/entries";
  private const string RaygunMessageQueueMaxVariable = "RAYGUN_MESSAGE_QUEUE_MAX";

  // ReSharper disable once PublicConstructorInAbstractClass
  public RaygunSettingsBase()
  {
    // See if there's an overload defined in an environment variable, and set it accordingly
    var messageQueueMaxValue = Environment.GetEnvironmentVariable(RaygunMessageQueueMaxVariable);
    if (!string.IsNullOrEmpty(messageQueueMaxValue) && int.TryParse(messageQueueMaxValue, out var maxQueueSize))
    {
      BackgroundMessageQueueMax = maxQueueSize;
    }
  }

  /// <summary>
  /// Raygun Application API Key, can be found in the Raygun application dashboard by clicking the "Application settings" button
  /// </summary>
  public string ApiKey { get; set; }
 
  public Uri ApiEndpoint { get; set; } = new (DefaultApiEndPoint);

  public bool ThrowOnError { get; set; }

  public string ApplicationVersion { get; set; }

  /// <summary>
  /// If set to true will automatically setup handlers to catch Unhandled Exceptions
  /// </summary>
  /// <remarks>
  /// Currently defaults to false. This may be change in future releases.
  /// </remarks>
  public bool CatchUnhandledExceptions { get; set; } = false;

  /// <summary>
  /// The maximum queue size for background exceptions
  /// </summary>
  public int BackgroundMessageQueueMax { get; } = ushort.MaxValue;

  /// <summary>
  /// Controls the number of background threads used to process the raygun message queue
  /// </summary>
  /// <remarks>
  /// Defaults to Environment.ProcessorCount * 2 &gt;= 8 ? 8 : Environment.ProcessorCount * 2
  /// </remarks>
  public int BackgroundMessageWorkerCount { get; set; } = Environment.ProcessorCount * 2 >= 8 ? 8 : Environment.ProcessorCount * 2;

  /// <summary>
  /// A list of Environment Variables to include with the message.
  /// </summary>
  /// <remarks>
  /// Can include wildcards to support Exact, StartsWith, EndsWith and Contains matching.
  /// <br />
  /// Example: "PATH", "TEM*", "*EM*", "*EMP"
  /// <br />
  /// Passing in * will be ignored as we do not want to support collecting 'all' environment variables for security reasons.
  /// </remarks>
  public IList<string> EnvironmentVariables { get; set; } = new List<string>();
}