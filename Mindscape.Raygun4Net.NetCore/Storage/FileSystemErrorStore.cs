using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

public class FileSystemErrorStore : IOfflineErrorStore
{
  private readonly string _storageDirectory;

  public FileSystemErrorStore(string storageDirectory)
  {
    _storageDirectory = storageDirectory;
  }

  public virtual async Task<List<OfflineErrorRecord>> GetAll(CancellationToken cancellationToken)
  {
    var crashFiles = Directory.GetFiles(_storageDirectory, "*.crash");
    var errorRecords = new List<OfflineErrorRecord>();

    foreach (var crashFile in crashFiles)
    {
      try
      {
        using var fileStream = new FileStream(crashFile, FileMode.Open, FileAccess.Read);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream, Encoding.UTF8);

        Trace.WriteLine($"Attempting to load offline crash at {crashFile}");
        var jsonString = await reader.ReadToEndAsync();
        var errorRecord = SimpleJson.DeserializeObject<OfflineErrorRecord>(jsonString);

        errorRecords.Add(errorRecord);
      }
      catch (Exception ex)
      {
        Debug.WriteLine("Error deserializing offline crash: {0}", ex.ToString());
      }
    }

    return errorRecords;
  }

  public virtual async Task<bool> Save(OfflineErrorRecord errorRecord, CancellationToken cancellationToken)
  {
    try
    {
      Directory.CreateDirectory(_storageDirectory);

      var filePath = GetFilePath(errorRecord.Id);
      var jsonContent = SimpleJson.SerializeObject(errorRecord);

      using var fileStream = new FileStream(filePath, FileMode.Create);
      using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
      using var writer = new StreamWriter(gzipStream, Encoding.UTF8);

      Trace.WriteLine($"Saving crash {errorRecord.Id} to {filePath}");
      await writer.WriteAsync(jsonContent);
      await writer.FlushAsync();

      return true;
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error adding crash [{errorRecord.Id}] to store: {ex}");
      return false;
    }
  }

  public virtual Task<bool> Remove(Guid errorId, CancellationToken cancellationToken)
  {
    try
    {
      var filePath = GetFilePath(errorId);
      if (File.Exists(filePath))
      {
        File.Delete(filePath);
        return Task.FromResult(true);
      }
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error remove crash [{errorId}] from store: {ex}");
    }

    return Task.FromResult(false);
  }

  private string GetFilePath(Guid errorId)
  {
    return Path.Combine(_storageDirectory, $"raygun-cr-{errorId}.crash");
  }
}