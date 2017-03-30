using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunBreadcrumbs : IEnumerable<RaygunBreadcrumb>
  {
    public enum Level
    {
      Default = 0,
      Debug,
      Info,
      Warning,
      Error,
    }

    private readonly IRaygunBreadcrumbStorage _storage;

    public RaygunBreadcrumbs(IRaygunBreadcrumbStorage storage)
    {
      _storage = storage;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerator<RaygunBreadcrumb> GetEnumerator()
    {
      return _storage.GetEnumerator();
    }

    public void Record(string message)
    {
      Record(new RaygunBreadcrumb() { Message = message });
    }

    public void Record(RaygunBreadcrumb crumb)
    {
      if (crumb.Level == Level.Default)
        crumb.Level = Level.Info;
 
      if (ShouldRecord(crumb))
        _storage.Store(crumb);
    }

    public void Clear()
    {
      _storage.Clear();
    }

    private bool ShouldRecord(RaygunBreadcrumb crumb)
    {
      return crumb.Level >= RaygunSettings.Settings.BreadcrumbsLevel;
    }
  }
}
