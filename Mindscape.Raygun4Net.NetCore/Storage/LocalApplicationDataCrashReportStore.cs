using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.Storage;

/// <summary>
/// Stores a cached copy of crash reports that failed to send in Local App Data
/// Creates a directory if specified, otherwise creates a unique directory based off the location of the application
/// </summary>
public sealed class LocalApplicationDataCrashReportStore : FileSystemCrashReportStore
{
  public LocalApplicationDataCrashReportStore(string directoryName = null, int maxOfflineFiles = 50)
    : base(GetLocalAppDirectory(directoryName), maxOfflineFiles)
  {
  }

  private static string GetLocalAppDirectory(string directoryName)
  {
    directoryName ??= CreateUniqueDirectory();
    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), directoryName);
  }

  private static string CreateUniqueDirectory()
  {
    // Try to generate a unique id, from the executable location
    var uniqueId = Assembly.GetEntryAssembly()?.Location ?? throw new ApplicationException("Cannot determine unique application id");

    var uniqueIdHash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(uniqueId));
    return BitConverter.ToString(uniqueIdHash).Replace("-", "").ToLowerInvariant();
  }
}