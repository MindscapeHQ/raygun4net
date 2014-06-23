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

    public RaygunRequestMessageOptions() { }

    public RaygunRequestMessageOptions(IEnumerable<string> formFieldNames, IEnumerable<string> headerNames, IEnumerable<string> cookieNames, IEnumerable<string> serverVariableNames)
    {
      _ignoredFormFieldNames.AddRange(formFieldNames);
      _ignoreHeaderNames.AddRange(headerNames);
      _ignoreCookieNames.AddRange(cookieNames);
      _ignoreServerVariableNames.AddRange(serverVariableNames);
    }

    public List<string> IgnoreFormFieldNames { get { return _ignoredFormFieldNames; } }

    public List<string> IgnoreHeaderNames { get { return _ignoreHeaderNames; } }

    public List<string> IgnoreCookieNames { get { return _ignoreCookieNames; } }

    public List<string> IgnoreServerVariableNames { get { return _ignoreServerVariableNames; } }
  }
}
