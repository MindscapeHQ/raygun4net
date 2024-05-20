using System;

namespace Mindscape.Raygun4Net.Diagnostics;

public sealed class PEDebugInformation
{
  /// <summary>
  /// The signature of the PE and PDB linking them together - usually a GUID
  /// </summary>
  public string Signature { get; internal set; }
  
  /// <summary>
  /// Checksum of the PE & PDB. Format: {algorithm}:{hash:X}
  /// </summary>
  public string Checksum { get; internal set; }
  
  /// <summary>
  /// The full location of the PDB at build time
  /// </summary>
  public string File { get; internal set; }
  
  /// <summary>
  /// The generated Timestamp of the code at build time stored as hex
  /// </summary>
  public string Timestamp { get; internal set; }
  
}