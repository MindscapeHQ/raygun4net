using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Mindscape.Raygun4Net
{
  internal class RaygunBreadcrumbs : IRaygunBreadcrumbStorage
  {
    private readonly RaygunSettings _settings;
    private readonly IRaygunBreadcrumbStorage _internalStorage;

    public RaygunBreadcrumbs(RaygunSettings settings, IRaygunBreadcrumbStorage storage)
    {
      _settings = settings;
      _internalStorage = storage;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerator<RaygunBreadcrumb> GetEnumerator()
    {
      return _internalStorage.GetEnumerator();
    }
    
    public void Store(RaygunBreadcrumb crumb)
    {
      if (_settings.BreadcrumbsLocationRecordingEnabled)
      {
        try
        {
          for (int i = 1; i <= 3; i++)
          {
            PopulateLocation(crumb, i);
            
            if (crumb.ClassName == null || !crumb.ClassName.StartsWith("Mindscape.Raygun4Net"))
            {
              break;
            }
          }
        }
        catch (Exception)
        {
          if (_settings.ThrowOnError)
          {
            throw;
          }
        }
      }

      if (ShouldRecord(crumb))
      {
        _internalStorage.Store(crumb);
      }
    }

    private void PopulateLocation(RaygunBreadcrumb crumb, int stackTraceFrame)
    {
      #if NETSTANDARD2_0
      var frame = new StackFrame(stackTraceFrame);
      var method = frame.GetMethod();

      crumb.ClassName = method.ReflectedType == null ? null : method.ReflectedType.FullName;
      crumb.MethodName = method.Name;
      crumb.LineNumber = frame.GetFileLineNumber();
      
      if (crumb.MethodName.Contains("<"))
      {
        var unmangledName = new Regex(@"<(\w+)>").Match(crumb.MethodName).Groups[1].Value;
        crumb.MethodName = unmangledName;
      }

      if (crumb.LineNumber == 0)
      {
        crumb.LineNumber = null;
      }
      #endif
    }

    public void Clear()
    {
      _internalStorage.Clear();
    }

    private bool ShouldRecord(RaygunBreadcrumb crumb)
    {
      return crumb.Level >= _settings.BreadcrumbsLevel;
    }
  }
}