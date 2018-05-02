using Mindscape.Raygun4Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tests.Model // Namespace dowse not contain Mindscape.Raygun in order to be ignored by the breadcrumb stacktrace scanning logic.
{
  internal class RaygunBreadcrumbTestRecorder
  {
    private RaygunBreadcrumbs _breadcrumbs;

    public RaygunBreadcrumbTestRecorder(RaygunBreadcrumbs breadcrumbs)
    {
      _breadcrumbs = breadcrumbs;
    }

    public void Record()
    {
      _breadcrumbs.Store(new Mindscape.Raygun4Net.RaygunBreadcrumb() { Message = "test" });
    }
  }
}
