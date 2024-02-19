using System.Runtime.CompilerServices;
using Mindscape.Raygun4Net.Breadcrumbs;

namespace Mindscape.Raygun4Net.NetCore.Tests;

public class BreadcrumbTestRecorder
{
  [MethodImpl(MethodImplOptions.NoInlining)]
  public void Record()
  {
    RaygunBreadcrumbs.Record(new RaygunBreadcrumb() { Message = "test" });
  }
}