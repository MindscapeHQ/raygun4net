using System;
using System.Runtime.Serialization;

namespace Mindscape.Raygun4Net.Storage;

public sealed class CrashReportStoreEntry
{
  /// <summary>
  /// Unique ID for the record
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// The application api key that the payload was intended for
  /// </summary>
  public string ApiKey { get; set; }

  /// <summary>
  /// The JSON serialized payload of the error
  /// </summary>
  public string MessagePayload { get; set; }
}