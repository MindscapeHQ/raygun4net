using System;
using System.Runtime.CompilerServices;

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using System.Web;
#endif

namespace Mindscape.Raygun4Net
{
  public static class APM
  {
    [ThreadStatic] private static bool _enabled = false;

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
      get
      {
#if NETSTANDARD1_6 || NETSTANDARD2_0
        // Look for .NET CORE compatible Environment Variables
        return
          Environment.GetEnvironmentVariable("CORECLR_PROFILER") == "{e2338988-38cc-48cd-a6b6-b441c31f34f1}" &&
          Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING") == "1";
#else
        // Look for .NET FRAMEWORK compatible Environment Variables
        return
          Environment.GetEnvironmentVariable("COR_PROFILER") == "{e2338988-38cc-48cd-a6b6-b441c31f34f1}" &&
          Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING") == "1";
#endif
      }
    }

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
    /// <summary>
    /// Initializes enhanced features when using Raygun APM.
    /// Should be called once on Application init. Subsequent calls will do nothing once initialized.
    /// This will be called automatically by the RaygunHttpModule if configured.
    /// </summary>
    /// <param name="context"></param>
    public static void Initialize(HttpApplication context, string appIdentifier = null)
    {
      var initializer = new HttpApplicationInitializer();
      initializer.Initialize(context, appIdentifier);
    }
#endif
  }
}
