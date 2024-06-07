using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Offline;

namespace Mindscape.Raygun4Net.Storage;

public class FileSystemCrashReportStore : OfflineStoreBase
{
  private const string CacheFileExtension = "rgcrash";
  private readonly string _storageDirectory;
  private readonly int _maxOfflineFiles;
  private readonly ConcurrentDictionary<Guid, string> _cacheLocationMap = new();

  public FileSystemCrashReportStore(IBackgroundSendStrategy backgroundSendStrategy, string storageDirectory, int maxOfflineFiles = 50)
    : base(backgroundSendStrategy)
  {
    _storageDirectory = storageDirectory;
    _maxOfflineFiles = maxOfflineFiles;
  }

  public override async Task<List<CrashReportStoreEntry>> GetAll(CancellationToken cancellationToken)
  {
    var crashFiles = Directory.GetFiles(_storageDirectory, $"*.{CacheFileExtension}");
    var errorRecords = new List<CrashReportStoreEntry>();

    foreach (var crashFile in crashFiles)
    {
      try
      {
        using var fileStream = new FileStream(crashFile, FileMode.Open, FileAccess.Read);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);

        Trace.WriteLine($"Attempting to load offline crash at {crashFile}");
        var jsonString = await reader.ReadToEndAsync();
        var errorRecord = SimpleJson.DeserializeObject<CrashReportStoreEntry>(jsonString);

        errorRecords.Add(errorRecord);
        _cacheLocationMap[errorRecord.Id] = crashFile;
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error deserializing offline crash: {0}", ex.ToString());
        File.Move(crashFile, $"{crashFile}.failed");
      }
    }

    return errorRecords;
  }

  public override async Task<bool> Save(string payload, string apiKey, CancellationToken cancellationToken)
  {
    var cacheEntryId = Guid.NewGuid();
    try
    {
      Directory.CreateDirectory(_storageDirectory);

      var crashFiles = Directory.GetFiles(_storageDirectory, $"*.{CacheFileExtension}");
      if (crashFiles.Length >= _maxOfflineFiles)
      {
        Debug.WriteLine($"Maximum offline files of [{_maxOfflineFiles}] has been reached");
        return false;
      }

      var cacheEntry = new CrashReportStoreEntry
      {
        Id = cacheEntryId,
        ApiKey = apiKey,
        MessagePayload = payload
      };
      var filePath = GetFilePathForCacheEntry(cacheEntryId);
      var jsonContent = SimpleJson.SerializeObject(cacheEntry);

      using var fileStream = new FileStream(filePath, FileMode.Create);
      using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
      using var writer = new StreamWriter(gzipStream, Encoding.UTF8);

      Trace.WriteLine($"Saving crash {cacheEntry.Id} to {filePath}");
      await writer.WriteAsync(jsonContent);
      await writer.FlushAsync();

      return true;
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error adding crash [{cacheEntryId}] to store: {ex}");
      return false;
    }
  }

  public override Task<bool> Remove(Guid cacheId, CancellationToken cancellationToken)
  {
    try
    {
      if (_cacheLocationMap.TryGetValue(cacheId, out var filePath))
      {
        var result = RemoveFile(filePath);
        _cacheLocationMap.TryRemove(cacheId, out _);
        return Task.FromResult(result);
      }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error remove crash [{cacheId}] from store: {ex}");
    }

    return Task.FromResult(false);
  }

  private static bool RemoveFile(string filePath)
  {
    if (!File.Exists(filePath))
    {
      return false;
    }

    File.Delete(filePath);
    return true;
  }

  private string GetFilePathForCacheEntry(Guid cacheId)
  {
    return Path.Combine(_storageDirectory, $"{cacheId:N}.{CacheFileExtension}");
  }
}