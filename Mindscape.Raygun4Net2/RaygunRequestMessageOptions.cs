using System;
using System.Collections.Generic;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunRequestMessageOptions
  {
    private readonly List<string> _ignoredFormFieldNames = new List<string>();
    private readonly List<string> _ignoreHeaderNames = new List<string>();
    private readonly List<string> _ignoreCookieNames = new List<string>();
    private readonly List<string> _ignoreServerVariableNames = new List<string>();
    private bool _isRawDataIgnored;

    public RaygunRequestMessageOptions() { }

    public RaygunRequestMessageOptions(IEnumerable<string> formFieldNames, IEnumerable<string> headerNames, IEnumerable<string> cookieNames, IEnumerable<string> serverVariableNames)
    {
      Add(_ignoredFormFieldNames, formFieldNames);
      Add(_ignoreHeaderNames, headerNames);
      Add(_ignoreCookieNames, cookieNames);
      Add(_ignoreServerVariableNames, serverVariableNames);
    }

    // RawData

    public bool IsRawDataIgnored
    {
      get { return _isRawDataIgnored; }
      set
      {
        _isRawDataIgnored = value;
      }
    }

    // Form fields

    public void AddFormFieldNames(params string[] names)
    {
      Add(_ignoredFormFieldNames, names);
    }

    public bool IsFormFieldIgnored(string name)
    {
      return IsIgnored(name, _ignoredFormFieldNames);
    }

    // Headers

    public void AddHeaderNames(params string[] names)
    {
      Add(_ignoreHeaderNames, names);
    }

    public bool IsHeaderIgnored(string name)
    {
      return IsIgnored(name, _ignoreHeaderNames);
    }

    // Cookies

    public void AddCookieNames(params string[] names)
    {
      Add(_ignoreCookieNames, names);
    }

    public bool IsCookieIgnored(string name)
    {
      return IsIgnored(name, _ignoreCookieNames);
    }

    // Server variables

    public void AddServerVariableNames(params string[] names)
    {
      Add(_ignoreServerVariableNames, names);
    }

    public bool IsServerVariableIgnored(string name)
    {
      return IsIgnored(name, _ignoreServerVariableNames);
    }

    // Core methods:

    private void Add(List<string> list, IEnumerable<string> names)
    {
      foreach (string name in names)
      {
        if (name != null)
        {
          list.Add(name.ToLower());
        }
      }
    }

    private bool IsIgnored(string name, List<string> list)
    {
      if (name == null || (list.Count == 1 && "*".Equals(list[0])))
      {
        return true;
      }

      foreach (string ignore in list)
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
  }
}