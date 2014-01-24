using System;
using System.Runtime.InteropServices;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunSystemInfoWrapper
  {
    [DllImport("kernel32.dll")]
    public static extern void GetNativeSystemInfo(ref SYSTEM_INFO systemInfo);
  }

  public struct SYSTEM_INFO
  {
    public PROCESSOR_ARCHITECTURE wProcessorArchitecture;
    public PROCESSOR_TYPE dwProcessorType;
    private ushort wReserved;
    private uint dwPageSize;
    public IntPtr lpMinimumApplicationAddress;
    public IntPtr lpMaximumApplicationAddress;
    public IntPtr dwActiveProcessorMask;
    public uint dwNumberOfProcessors;
    public uint dwAllocationGranularity;
    public ushort wProcessorLevel;
    public ushort wProcessorRevision;
  }

  public enum PROCESSOR_ARCHITECTURE
  {
    PROCESSOR_ARCHITECTURE_AMD64 = 9,
    PROCESSOR_ARCHITECTURE_ARM = 4,
    PROCESSOR_ARCHITECTURE_IA64 = 6,
    PROCESSOR_ARCHITECTURE_INTEL = 0,
    PROCESSOR_ARCHITECTURE_UNKNOWN = 0xffff
  }

  public enum PROCESSOR_TYPE
  {
    PROCESSOR_INTEL_386,
    PROCESSOR_INTEL_486,
    PROCESSOR_INTEL_PENTIUM,
    PROCESSOR_INTEL_IA64,
    PROCESSOR_AMD_X8664,
    PROCESSOR_ARM
  }
}
