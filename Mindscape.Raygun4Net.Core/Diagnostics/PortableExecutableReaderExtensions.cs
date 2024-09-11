using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace Mindscape.Raygun4Net.Diagnostics;

internal static class PortableExecutableReaderExtensions
{
  public static PEReader GetFileSystemPEReader(string moduleName)
  {
    try
    {
      // Read into memory to avoid any premature stream closures
      var bytes = ImmutableArray.Create(File.ReadAllBytes(moduleName));
      return new PEReader(bytes);
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Could not open module [{moduleName}] from disk: {ex}");
      return null;
    }
  }

  public static bool TryGetDebugInformation(this PEReader peReader, out PEDebugInformation debugInformation)
  {
    if (peReader is null)
    {
      debugInformation = null;
      return false;
    }

    try
    {
      debugInformation = GetDebugInformation(peReader);
      return true;
    }
    catch (Exception ex)
    {
      Debug.WriteLine($"Error reading PE Debug Data: {ex}");
    }

    debugInformation = null;
    return false;
  }

  private static PEDebugInformation GetDebugInformation(this PEReader peReader)
  {
    var debugInfo = new PEDebugInformation
    {
      Timestamp = $"{peReader.PEHeaders.CoffHeader.TimeDateStamp:X8}"
    };

    foreach (var entry in peReader.ReadDebugDirectory())
    {
      if (entry.Type == DebugDirectoryEntryType.CodeView)
      {
        // Read the CodeView data
        var codeViewData = peReader.ReadCodeViewDebugDirectoryData(entry);

        debugInfo.File = codeViewData.Path;
        debugInfo.Signature = codeViewData.Guid.ToString();
      }

      if (entry.Type == DebugDirectoryEntryType.PdbChecksum)
      {
        var checksumEntry = peReader.ReadPdbChecksumDebugDirectoryData(entry);
        var checksumHex = BitConverter.ToString(checksumEntry.Checksum.ToArray()).Replace("-", "").ToUpperInvariant();
        debugInfo.Checksum = $"{checksumEntry.AlgorithmName}:{checksumHex}";
      }
    }

    return debugInfo;
  }
}