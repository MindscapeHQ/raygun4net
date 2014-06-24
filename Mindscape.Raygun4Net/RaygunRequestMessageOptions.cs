using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Mindscape.Raygun4Net
{
  public class RaygunRequestMessageOptions
  {
    private readonly List<string> _ignoredFormFieldNames = new List<string>();
    private readonly List<string> _ignoreHeaderNames = new List<string>();
    private readonly List<string> _ignoreCookieNames = new List<string>();
    private readonly List<string> _ignoreServerVariableNames = new List<string>();

    public RaygunRequestMessageOptions() { }

    public RaygunRequestMessageOptions(IEnumerable<string> formFieldNames, IEnumerable<string> headerNames, IEnumerable<string> cookieNames, IEnumerable<string> serverVariableNames)
    {
      _ignoredFormFieldNames.AddRange(formFieldNames);
      _ignoreHeaderNames.AddRange(headerNames);

      foreach (string name in cookieNames)
      {
        AddCookieNames(name);
      }

      _ignoreServerVariableNames.AddRange(serverVariableNames);
    }

    public List<string> IgnoreFormFieldNames { get { return _ignoredFormFieldNames; } }

    public List<string> IgnoreHeaderNames { get { return _ignoreHeaderNames; } }

    public void AddCookieNames(params string[] names)
    {
      foreach (string name in names)
      {
        if (name != null)
        {
          _ignoreCookieNames.Add(name.ToLower());
        }
      }
    }

    public bool IsCookieIgnored(string name)
    {
      if (name == null || (_ignoreCookieNames.Count == 1 && "*".Equals(_ignoreCookieNames[0])))
      {
        return true;
      }

      foreach (string ignore in _ignoreCookieNames)
      {
        string lowerName = name.ToLower();
        if (ignore.StartsWith("*"))
        {
          if (ignore.EndsWith("*"))
          {
            if (lowerName.Contains(ignore.Substring(1, ignore.Length - 2)))
            {
              return true;
            }
          }
          else
          {
            if (lowerName.EndsWith(ignore.Substring(1)))
            {
              return true;
            }
          }
        }
        else if (ignore.EndsWith("*") && lowerName.StartsWith(ignore.Substring(0, ignore.Length - 1)))
        {
          return true;
        }
        else if (lowerName.Equals(ignore))
        {
          return true;
        }
      }

      return false;
    }

    public List<string> IgnoreServerVariableNames { get { return _ignoreServerVariableNames; } }
  }
}
