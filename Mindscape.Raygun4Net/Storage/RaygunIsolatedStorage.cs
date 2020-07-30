using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Text;

namespace Mindscape.Raygun4Net.Storage
{
  public class RaygunIsolatedStorage : IRaygunOfflineStorage
  {
    private static string _folderNameHash = null;

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
        // Get the directory within isolated storage to hold our data.
        var localDirectory = GetLocalDirectory(apiKey);

        if (string.IsNullOrEmpty(localDirectory))
        {
          return false;
        }

        // Create the destination if it's not there.
        if (!EnsureDirectoryExists(storage, localDirectory))
        {
          return false;
        }

        var searchPattern = Path.Combine(localDirectory, $"*{RaygunFileFormat}");
        var maxReports = Math.Min(maxReportsStored, MaxStoredReportsHardUpperLimit);

        // We can only save the report if we havn't reached the report count limit.
        if (storage.GetFileNames(searchPattern).Length >= maxReports)
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

    private bool EnsureDirectoryExists(IsolatedStorageFile storage, string localDirectory)
    {
      bool success = true;

      try
      {
        storage.CreateDirectory(localDirectory);
      }
      catch
      {
        success = false;
      }

      return success;
    }

    public IList<IRaygunFile> FetchAll(string apiKey)
    {
      var files = new List<IRaygunFile>();

      using (var storage = GetIsolatedStorageScope())
      {
        // Get the directory within isolated storage to hold our data.
        var localDirectory = GetLocalDirectory(apiKey);

        if (string.IsNullOrEmpty(localDirectory))
        {
          return files;
        }

        // We must ensure the local directory exists before we look for files.
        if (storage.GetDirectoryNames(localDirectory)?.Length == 0)
        {
          return files;
        }

        // Look for all the files within our working local directory.
        var fileNames = storage.GetFileNames(Path.Combine(localDirectory, $"*{RaygunFileFormat}"));

        // Take action on each file.
        foreach (var name in fileNames)
        {
          var stream = new IsolatedStorageFileStream(Path.Combine(localDirectory, name), FileMode.Open, storage);

          // Read the contents and put it into our own structure.
          using (var reader = new StreamReader(stream))
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

      using (var storage = GetIsolatedStorageScope())
      {
        // Get a list of the current files in storage.
        var localDirectory = GetLocalDirectory(apiKey);

        if (string.IsNullOrEmpty(localDirectory))
        {
          return false;
        }

        var localFilePath = Path.Combine(localDirectory, name);

        // We must ensure the local file exists before delete it.
        if (storage.GetFileNames(localFilePath)?.Length == 0)
        {
          return false;
        }

        storage.DeleteFile(localFilePath);

        return true;
      }
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
      // Attempt to perform the hash operation once.
      if (_folderNameHash == null)
      {
        _folderNameHash = PerformHash(apiKey);
      }

      // If successful return the correct path.
      return (_folderNameHash == null) ? null : Path.Combine(RaygunBaseDirectory, _folderNameHash);
    }

    private string PerformHash(string input)
    {
      // Use input string to calculate MD5 hash
      using (var hasher = MD5.Create())
      {
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = hasher.ComputeHash(inputBytes);

        // Convert the byte array to hexadecimal string
        var builder = new StringBuilder();

        foreach (var hashByte in hashBytes)
        {
          builder.Append(hashByte.ToString("X2"));
        }

        return builder.ToString();
      }
    }

    private string GetUniqueAcendingJsonName()
    {
      return $"{DateTime.UtcNow.Ticks}-{_currentFileCounter++}-{Guid.NewGuid().ToString()}{RaygunFileFormat}";
    }
  }
}