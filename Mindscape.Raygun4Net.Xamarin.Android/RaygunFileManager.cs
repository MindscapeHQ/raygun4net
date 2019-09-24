using System.Text;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Messages;
using System;
using System.IO;
using Android.Content;
using Android.Runtime;

namespace Mindscape.Raygun4Net
{
  public class RaygunFileManager
  {
    public const int MAX_STORED_REPORTS_UPPER_LIMIT = 64;
    private const string RAYGUN_DIRECTORY = "RaygunIO";

    private int currentFileCounter = 0;

    public void Intialise()
    {
      try
      {
        var crashReportDirectory = GetCrashReportDirectory();

        if (!Directory.Exists(crashReportDirectory))
        {
          Directory.CreateDirectory(crashReportDirectory);
        }
      }
      catch (Exception e)
      {
        RaygunLogger.Error("Failed to initialise file manager due to error: " + e.Message);
      }
    }

    public string SaveCrashReport(RaygunMessage crashReport, int maxReportsStored)
    {
      try
      {
        // Can only save the report if we havnt reached the report count limit.
        if (IsFileLimitReached(Math.Min(maxReportsStored, MAX_STORED_REPORTS_UPPER_LIMIT)))
        {
          RaygunLogger.Warning("Failed to store crash report - Reached max crash reports stored on device");
          return null;
        }

        // Convert to JSON text.
        var reportJson = SimpleJson.SerializeObject(crashReport);

        // Generate a unique name for our report.
        var fileName = GetUniqueAcendingJsonName();

        // Save the report to disk.
        return StoreCrashReport(fileName, reportJson);
      }
      catch (Exception ex)
      {
        RaygunLogger.Error(string.Format("Failed to store crash report - Error saving message to isolated storage {0}", ex.Message));
      }

      return null;
    }

    private string StoreCrashReport(string fileName, string data)
    {
      // Generate the full file path.
      var filePath = Path.Combine(GetCrashReportDirectory(), fileName);

      // Write data to file path.
      using (var writer = File.Create(filePath))
      {
        var bytes = Encoding.ASCII.GetBytes(data);
        writer.Write(bytes, 0, bytes.Length);
      }

      return filePath;
    }

    public List<RaygunFile> GetAllStoredCrashReports()
    {
      var allReports = new List<RaygunFile>();

      try
      {
        // Get the paths to all the files in the crash report directory.
        var files = Directory.GetFiles(GetCrashReportDirectory());

        foreach (var file in files)
        {
          // Generate the full file path.
          var filePath = Path.Combine(GetCrashReportDirectory(), file);

          // Read the content of the file on disk.
          var raygunFile = ReadCrashReportFromDisk(filePath);
         
          // Add it to our list if valid.
          if (raygunFile != null && raygunFile.Data != null)
          {
            allReports.Add(raygunFile);
          }
        }
      }
      catch (Exception ex)
      {
        RaygunLogger.Error(string.Format("Error sending stored messages to Raygun.io {0}", ex.Message));
      }

      return allReports;
    }

    private RaygunFile ReadCrashReportFromDisk(string filePath)
    {
      // Check a file exists with this path.
      if (filePath == null || !File.Exists(filePath))
      {
        return null;
      }

      var raygunFile = new RaygunFile(filePath);

      // Read in the contents of the file.
      using (var streamReader = new StreamReader(filePath))
      {
        raygunFile.Data = streamReader.ReadToEnd();
      }

      return raygunFile;
    }

    public void RemoveFile(string filePath)
    {
      // Validate the file path.
      if (string.IsNullOrEmpty(filePath))
      {
        RaygunLogger.Debug("Failed to remove file - Invalid file path");
        return;
      }

      // Check a file exists at this path.
      if (!File.Exists(filePath))
      {
        RaygunLogger.Debug("Failed to remove file - File does not exist");
        return;
      }

      try
      {
        File.Delete(filePath);
      }
      catch(Exception e)
      {
        RaygunLogger.Error("Failed to remove file - Due to error: " + e.Message); 
      }
    }

    public void RemoveFiles(List<RaygunFile> files)
    {
      foreach (var file in files)
      {
        if (file != null)
        {
          RemoveFile(file.Path);
        }
      }
    }

    private bool IsFileLimitReached(int maxCount)
    {
      return NumberOfCrashReportsOnDisk() >= maxCount;
    }

    private int NumberOfCrashReportsOnDisk()
    {
      var files = Directory.GetFiles(GetCrashReportDirectory());
      return files.Length;
    }

    private string GetCrashReportDirectory()
    {
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), RAYGUN_DIRECTORY);
    }

    private string GetUniqueAcendingJsonName()
    {
      return string.Format("{0}-{1}-{2}.json", DateTime.UtcNow.Ticks, currentFileCounter++, Guid.NewGuid().ToString());
    }
  }
}
