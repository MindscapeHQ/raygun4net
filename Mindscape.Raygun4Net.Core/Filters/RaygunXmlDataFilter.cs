using System;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Filters
{
  public class RaygunXmlDataFilter : IRaygunDataFilter
  {
    public bool CanParse(string data)
    {
      throw new NotImplementedException();
    }

    public string Filter(string data, IList<string> sensitiveFields)
    {
      throw new NotImplementedException();
    }
  }
}
