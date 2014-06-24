using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Mindscape.Raygun4Net
{
  public class RaygunRequestMessageOptions
  {
    private readonly List<string> _ignoredFormFieldNames = new List<string>();
    private readonly List<Regex> _formFieldExpressions = new List<Regex>();

    private readonly List<string> _ignoreHeaderNames = new List<string>();

    private readonly List<string> _ignoreCookieNames = new List<string>();
    private readonly List<Regex> _cookieExpressions = new List<Regex>();

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

    //public List<string> IgnoreCookieNames { get { return _ignoreCookieNames; } }
    public void AddCookieNames(params string[] names)
    {
      foreach (string name in names)
      {
        try
        {
          Regex regex = new Regex(name);
          _cookieExpressions.Add(regex);
        }
        catch
        {
          _ignoreCookieNames.AddRange(names);
        }
      }
    }

    public bool IsCookieIgnored(string name)
    {
      if (_ignoreCookieNames.Count == 1 && "*".Equals(_ignoreCookieNames[0]))
      {
        return true;
      }

      if (_ignoreCookieNames.Contains(name))
      {
        return true;
      }

      foreach (Regex regex in _cookieExpressions)
      {
        Match match = regex.Match(name);
        if (match != null && match.Success)
        {
          return true;
        }
      }
      return false;
    }

    public List<string> IgnoreServerVariableNames { get { return _ignoreServerVariableNames; } }
  }
}
