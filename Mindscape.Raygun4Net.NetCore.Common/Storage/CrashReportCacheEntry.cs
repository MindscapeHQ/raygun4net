using System;
using System.Runtime.Serialization;

namespace Mindscape.Raygun4Net.Storage;

public class CrashReportCacheEntry
{
  /// <summary>
  /// Unique ID for the record
  /// </summary>
  public Guid Id { get; }

  /// <summary>
  /// The application api key that the payload was intended for
  /// </summary>
  public string ApiKey { get; }

  public string Payload { get; }

  /// <summary>
  /// The JSON serialized payload of the error
  /// </summary>
  public string MessagePayload { get; }

  /// <summary>
  /// The location of the cache entry - most likely the path on disk
  /// </summary>
  [IgnoreDataMember]
  public string Location { get; set; }

  public CrashReportCacheEntry(string apiKey, string payload, string location = null)
    : this(Guid.NewGuid(), apiKey, payload, location)
  {
  }

  public CrashReportCacheEntry(Guid id, string apiKey, string payload, string location = null)
  {
    Id = id;
    ApiKey = apiKey;
    Payload = payload;
    Location = location;
  }
}