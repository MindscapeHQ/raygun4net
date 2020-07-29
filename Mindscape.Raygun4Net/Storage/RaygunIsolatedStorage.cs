using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Storage
{
  public class RaygunIsolatedStorage : IRaygunOfflineStorage
  {
    private const int MaxStoredReportsHardUpperLimit = 64;
    private const string RaygunBaseDirectory = "Raygun";
    private const string RaygunFileFormat = ".json";

    private int _currentFileCounter = 0;

    public bool Store(string message, string apiKey, int maxReportsStored)
    {
      // Do not store invalid messages.
      if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(apiKey))
      {
        return false;
      }

      using (var storage = GetIsolatedStorageScope())
      {
        var maxReports = Math.Min(maxReportsStored, MaxStoredReportsHardUpperLimit);

        // We can only save the report if we havn't reached the report count limit.
        if (IsLocalStorageFull(storage, apiKey, maxReports))
        {
          return false;
        }

        // Get the directory within isolated storage to hold our data.
        var localDirectory = GetLocalDirectory(apiKey);

        // Create the destination if it's not there.
        if (!EnsureDirectoryExists(storage, localDirectory))
        {
          return false;
        }

        // Build up our file information.
        var filename = GetUniqueAcendingJsonName();
        var localFilePath = Path.Combine(localDirectory, filename);

        // Write the contents to storage.
        using (var stream = new IsolatedStorageFileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.Write, storage))
        {
          using (var writer = new StreamWriter(stream, Encoding.Unicode))
          {
            writer.Write(message);
            writer.Flush();
            writer.Close();
          }
        }
      }

      return true;
    }

    private bool IsLocalStorageFull(IsolatedStorageFile storage, string apiKey, int maxCount)
    {
      return NumberOfFilesOnDisk(storage, apiKey) >= maxCount;
    }

    private int NumberOfFilesOnDisk(IsolatedStorageFile storage, string apiKey)
    {
      var files = storage.GetFileNames(Path.Combine(GetLocalDirectory(apiKey), $"*{RaygunFileFormat}"));

      if (files != null)
      {
        return files.Length;
      }

      return MaxStoredReportsHardUpperLimit;
    }

    private bool EnsureDirectoryExists(IsolatedStorageFile storage, string localDirectory)
    {
      bool success = true;

      try
      {
        storage.CreateDirectory(localDirectory);
      }
      catch (Exception ex)
      {
        success = false;
      }

      return success;
    }

    public IList<IRaygunFile> FetchAll(string apiKey)
    {
      var files = new List<IRaygunFile>();

      using (IsolatedStorageFile storage = GetIsolatedStorageScope())
      {
        // Get the directory within isolated storage to hold our data.
        var localDirectory = GetLocalDirectory(apiKey);

        // Look for all the files within our working local directory.
        var fileNames = storage.GetFileNames(Path.Combine(localDirectory, $"*{RaygunFileFormat}"));

        // Take action on each file.
        foreach (string name in fileNames)
        {
          var stream = new IsolatedStorageFileStream(Path.Combine(localDirectory, name), FileMode.Open, storage);

          // Read the contents and put it into our own structure.
          using (StreamReader reader = new StreamReader(stream))
          {
            string contents = reader.ReadToEnd();

            files.Add(new RaygunFile()
            {
              Name = name,
              Contents = contents
            });
          }
        }
      }

      return files;
    }

    public bool Remove(string name, string apiKey)
    {
      // We cannot remove based on invalid params.
      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(apiKey))
      {
        return false;
      }

      using (IsolatedStorageFile storage = GetIsolatedStorageScope())
      {
        // Get a list of the current files in storage.
        var localDirectory = GetLocalDirectory(apiKey);
        var localFileNames = storage.GetFileNames(localDirectory);

        // Check for a file with the same name.
        if (HasMatchingName(localFileNames, name))
        {
          // Remove the matching file from storage.
          storage.DeleteFile(Path.Combine(localDirectory, name));

          return true;
        }
      }

      return false;
    }

    private bool HasMatchingName(string[] fileNames, string name)
    {
      foreach (var fileName in fileNames)
      {
        if (fileName.Equals(name))
        {
          return true;
        }
      }
      return false;
    }

    private IsolatedStorageFile GetIsolatedStorageScope()
    {
      if (AppDomain.CurrentDomain != null && AppDomain.CurrentDomain.ActivationContext != null)
      {
        return IsolatedStorageFile.GetUserStoreForApplication();
      }
      else
      {
        return IsolatedStorageFile.GetUserStoreForAssembly();
      }
    }

    private string GetLocalDirectory(string apiKey)
    {
      // TODO encode the apikey
      return Path.Combine(RaygunBaseDirectory, apiKey);
    }

    private string GetUniqueAcendingJsonName()
    {
      return $"{DateTime.UtcNow.Ticks}-{_currentFileCounter++}-{Guid.NewGuid().ToString()}{RaygunFileFormat}";
    }
  }
}