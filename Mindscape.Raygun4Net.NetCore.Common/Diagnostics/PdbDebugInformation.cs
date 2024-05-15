using System;

namespace Mindscape.Raygun4Net.Diagnostics;

internal sealed class PdbDebugInformation
{
  /// <summary>
  /// The signature of the PE and PDB linking them together - usually a GUID
  /// </summary>
  public string Signature { get; set; }
  
  /// <summary>
  /// Checksum of the PE & PDB. Format: {algorithm}:{hash:X}
  /// </summary>
  public string Checksum { get; set; }
  
  /// <summary>
  /// The full location of the PDB at build time
  /// </summary>
  public string File { get; set; }
  
  /// <summary>
  /// The generated Timestamp of the code at build time stored as hex
  /// </summary>
  public string Timestamp { get; set; }
  
}