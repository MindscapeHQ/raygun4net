using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunEnvironmentMessage
  {
    public int ProcessorCount { get; set; }

    public string OSVersion { get; set; }

    public double WindowBoundsWidth { get; set; }

    public double WindowBoundsHeight { get; set; }

    public string ResolutionScale { get; set; }

    public string CurrentOrientation { get; set; }

    public string Architecture { get; set; }

    public string Model { get; set; }

    public ulong TotalPhysicalMemory { get; set; }

    public ulong AvailablePhysicalMemory { get; set; }

    public string DeviceName { get; set; }

    public double UtcOffset { get; set; }

    public string Locale { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunEnvironmentMessage: ProcessorCount={0}, OSVersion={1}, WindowBoundsWidth={2}, WindowBoundsHeight={3}, Architecture={4}, TotalPhysicalMemory={5}, AvailablePhysicalMemory={6}, UtcOffset={7}, Locale={8}]", ProcessorCount, OSVersion, WindowBoundsWidth, WindowBoundsHeight, Architecture, TotalPhysicalMemory, AvailablePhysicalMemory, UtcOffset, Locale);
    }
  }
}