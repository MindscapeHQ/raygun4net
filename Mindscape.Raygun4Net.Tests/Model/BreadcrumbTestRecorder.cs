using Mindscape.Raygun4Net.Breadcrumbs;
using System.Runtime.CompilerServices;

namespace Tests.Model // Namespace does not contain Mindscape.Raygun in order to be ignored by the breadcrumb stacktrace scanning logic.
{
  public class BreadcrumbTestRecorder
  {
    private RaygunBreadcrumbs _breadcrumbs;

    public BreadcrumbTestRecorder(RaygunBreadcrumbs breadcrumbs)
    {
      _breadcrumbs = breadcrumbs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Record()
    {
      _breadcrumbs.Record(new Mindscape.Raygun4Net.RaygunBreadcrumb() { Message = "test" });
    }
  }
}
