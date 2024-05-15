using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

public class FileSystemCrashReportCache : ICrashReportCache
{
  private const string CacheFileExtension = "rgcrash";
  private readonly string _storageDirectory;

  public FileSystemCrashReportCache(string storageDirectory)
  {
    _storageDirectory = storageDirectory;
  }

  public virtual async Task<List<CrashReportCacheEntry>> GetAll(CancellationToken cancellationToken)
  {
    var crashFiles = Directory.GetFiles(_storageDirectory, $"*.{CacheFileExtension}");
    var errorRecords = new List<CrashReportCacheEntry>();

    foreach (var crashFile in crashFiles)
    {
      try
      {
        using var fileStream = new FileStream(crashFile, FileMode.Open, FileAccess.Read);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);

        Trace.WriteLine($"Attempting to load offline crash at {crashFile}");
        var jsonString = await reader.ReadToEndAsync();
        var errorRecord = SimpleJson.DeserializeObject<CrashReportCacheEntry>(jsonString);
        errorRecord.Location = crashFile;

        errorRecords.Add(errorRecord);
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error deserializing offline crash: {0}", ex.ToString());
      }
    }

    return errorRecords;
  }

  public virtual async Task<CrashReportCacheEntry> Save(string payload, string apiKey,
    CancellationToken cancellationToken)
  {
    var cacheEntryId = Guid.NewGuid();
    try
    {
      Directory.CreateDirectory(_storageDirectory);

      var cacheEntry = new CrashReportCacheEntry(cacheEntryId, apiKey, payload);
      var filePath = GetFilePathForCacheEntry(cacheEntryId);
      var jsonContent = SimpleJson.SerializeObject(cacheEntry);

      using var fileStream = new FileStream(filePath, FileMode.Create);
      using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
      using var writer = new StreamWriter(gzipStream, Encoding.UTF8);

      Trace.WriteLine($"Saving crash {cacheEntry.Id} to {filePath}");
      await writer.WriteAsync(jsonContent);
      await writer.FlushAsync();

      return cacheEntry;
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error adding crash [{cacheEntryId}] to store: {ex}");
      return null;
    }
  }

  public virtual Task<bool> Remove(CrashReportCacheEntry cacheEntry, CancellationToken cancellationToken)
  {
    try
    {
      var result = RemoveFile(cacheEntry.Location);
      return Task.FromResult(result);
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error remove crash [{cacheEntry.Id}] from store: {ex}");
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