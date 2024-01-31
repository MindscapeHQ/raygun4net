using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Mindscape.Raygun4Net.Breadcrumbs
{
  public class RaygunBreadcrumbs : IEnumerable<RaygunBreadcrumb>
  {
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
    
    public void Record(RaygunBreadcrumb crumb)
    {
      if (RaygunSettings.Settings.BreadcrumbsLocationRecordingEnabled)
      {
        try
        {
          for(int i = 1; i <= 3; i++)
          {
            PopulateLocation(crumb, i);
            if (crumb.ClassName == null || !crumb.ClassName.StartsWith("Mindscape.Raygun4Net", StringComparison.OrdinalIgnoreCase))
            {
              break;
            }
          }
        }
        catch (Exception)
        {
          if (RaygunSettings.Settings.ThrowOnError)
          {
            throw;
          }
        }
      }

      if (ShouldRecord(crumb))
      {
        _storage.Store(crumb);
      }
    }

    private void PopulateLocation(RaygunBreadcrumb crumb, int stackTraceFrame)
    {
      var frame = new StackFrame(stackTraceFrame);
      var method = frame.GetMethod();

      crumb.ClassName = method.ReflectedType == null ? null : method.ReflectedType.FullName;
      crumb.MethodName = method.Name;
      crumb.LineNumber = frame.GetFileLineNumber();
      File.WriteAllText($"C:\\temp\\stack-{stackTraceFrame}.json", SimpleJson.SerializeObject(crumb));
      if (crumb.MethodName.Contains("<"))
      {
        var unmangledName = new Regex(@"<(\w+)>").Match(crumb.MethodName).Groups[1].Value;
        crumb.MethodName = unmangledName;
      }

      if (crumb.LineNumber == 0)
      {
        crumb.LineNumber = null;
      }
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
