using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Mindscape.Raygun4Net.Breadcrumbs
{
  public static class RaygunBreadcrumbs
  {
    private static IRaygunBreadcrumbStorage _storage = new AsyncLocalBreadcrumbStorage();

    public static IRaygunBreadcrumbStorage Storage
    {
      get => _storage;
      set => _storage = value ?? throw new ArgumentNullException(nameof(value), "Storage cannot be null.");
    }

    public static void Record(string message)
    {
      Record(new RaygunBreadcrumb { Message = message });
    }

    public static void Record(RaygunBreadcrumb crumb)
    {
      if (crumb.Message.Length > 500)
      {
        return;
      }

      if (string.IsNullOrEmpty(crumb.ClassName) || string.IsNullOrEmpty(crumb.MethodName))
      {
        try
        {
          for (int i = 1; i <= 3; i++)
          {
            PopulateLocation(crumb, i);
            if (crumb.ClassName == null ||
                !crumb.ClassName.StartsWith("Mindscape.Raygun4Net", StringComparison.OrdinalIgnoreCase))
            {
              break;
            }
          }
        }
        catch (Exception)
        {
          // ignored
        }
      }

      _storage.Store(crumb);
    }

    private static void PopulateLocation(RaygunBreadcrumb crumb, int stackTraceFrame)
    {
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
    }

    public static void Clear()
    {
      _storage.Clear();
    }

    public static IList<RaygunBreadcrumb> ToList()
    {
      return _storage.ToList();
    }
  }
}