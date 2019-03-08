using System.Collections.Generic;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net
{
  public class RaygunRequestMessageOptions
  {
    private readonly List<string> _ignoredSensitiveFieldNames = new List<string>();
    private readonly List<string> _ignoredQueryParameterNames = new List<string>();
    private readonly List<string> _ignoredFormFieldNames = new List<string>();
    private readonly List<string> _ignoreHeaderNames = new List<string>();
    private readonly List<string> _ignoreCookieNames = new List<string>();
    private readonly List<string> _ignoreServerVariableNames = new List<string>();
    private bool _isRawDataIgnored;
    private bool _isRawDataIgnoredWhenFilteringFailed;
    private bool _useXmlRawDataFilter;
    private bool _useKeyPairRawDataFilter;

    private List<IRaygunDataFilter> _rawDataFilters = new List<IRaygunDataFilter>();

    public RaygunRequestMessageOptions() { }

    public RaygunRequestMessageOptions(IEnumerable<string> sensitiveFieldNames, 
                                       IEnumerable<string> queryParameterNames, 
                                       IEnumerable<string> formFieldNames, 
                                       IEnumerable<string> headerNames, 
                                       IEnumerable<string> cookieNames, 
                                       IEnumerable<string> serverVariableNames)
    {
      Add(_ignoredSensitiveFieldNames, sensitiveFieldNames);
      Add(_ignoredQueryParameterNames, queryParameterNames);
      Add(_ignoredFormFieldNames, formFieldNames);
      Add(_ignoreHeaderNames, headerNames);
      Add(_ignoreCookieNames, cookieNames);
      Add(_ignoreServerVariableNames, serverVariableNames);
    }

    // Raw Data

    public bool IsRawDataIgnored
    {
      get { return _isRawDataIgnored; }
      set { _isRawDataIgnored = value; }
    }

    public bool IsRawDataIgnoredWhenFilteringFailed
    {
      get { return _isRawDataIgnoredWhenFilteringFailed; }
      set { _isRawDataIgnoredWhenFilteringFailed = value; }
    }

    public bool UseXmlRawDataFilter
    {
      get { return _useXmlRawDataFilter; }
      set { _useXmlRawDataFilter = value; }
    }

    public bool UseKeyPairRawDataFilter
    {
      get { return _useKeyPairRawDataFilter; }
      set { _useKeyPairRawDataFilter = value; }
    }

    public void AddRawDataFilter(IRaygunDataFilter filter)
    {
      if (filter != null)
      {
        _rawDataFilters.Add(filter);
      }
    }

    public List<IRaygunDataFilter> GetRawDataFilters()
    {
      return _rawDataFilters;
    }

    // Sensitive Fields

    public void AddSensitiveFieldNames(params string[] names)
    {
      Add(_ignoredSensitiveFieldNames, names);
    }

    public bool IsSensitiveFieldIgnored(string name)
    {
      return IsIgnored(name, _ignoredSensitiveFieldNames);
    }

    public List<string> SensitiveFieldNames()
    {
      return _ignoredSensitiveFieldNames;
    }

    // Query Parameters

    public void AddQueryParameterNames(params string[] names)
    {
      Add(_ignoredQueryParameterNames, names);
    }

    public bool IsQueryParameterIgnored(string name)
    {
      return IsIgnored(name, _ignoredQueryParameterNames);
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
