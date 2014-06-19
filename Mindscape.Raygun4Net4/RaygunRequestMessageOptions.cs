using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunRequestMessageOptions
  {
    private readonly List<string> _ignoredFormNames = new List<string>();
    private readonly List<string> _ignoreHeaderNames = new List<string>();
    private readonly List<string> _ignoreCookieNames = new List<string>();
    private readonly List<string> _ignoreServerVariableNames = new List<string>();

    public List<string> IgnoreFormDataNames { get { return _ignoredFormNames; } }

    public List<string> IgnoreHeaderNames { get { return _ignoreHeaderNames; } }

    public List<string> IgnoreCookieNames { get { return _ignoreCookieNames; } }

    public List<string> IgnoreServerVariableNames { get { return _ignoreServerVariableNames; } }
  }
}
