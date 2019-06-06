using System;
using System.Runtime.CompilerServices;

namespace Mindscape.Raygun4Net
{
  public static class APM
  {
    [ThreadStatic]
    private static bool _enabled = false;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Enable()
    {
      _enabled = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void Disable()
    {
      _enabled = false;
    }

    public static bool IsEnabled
    {
      get { return _enabled; }
    }

    public static bool ProfilerAttached
    {
      get { return Environment.GetEnvironmentVariable("COR_PROFILER") == "{e2338988-38cc-48cd-a6b6-b441c31f34f1}" && Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING") == "1"; }
    }
  }
}
