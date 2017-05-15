using Mindscape.Raygun4Net.Breadcrumbs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tests.Model // Namespace dowse not contain Mindscape.Raygun in order to be ignored by the breadcrumb stacktrace scanning logic.
{
  internal class BreadcrumbTestRecorder
  {
    private RaygunBreadcrumbs _breadcrumbs;

    public BreadcrumbTestRecorder(RaygunBreadcrumbs breadcrumbs)
    {
      _breadcrumbs = breadcrumbs;
    }

    public void Record()
    {
      _breadcrumbs.Record(new Mindscape.Raygun4Net.RaygunBreadcrumb() { Message = "test" });
    }
  }
}
