using System;
using System.Linq;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Filters
{
  public class RaygunJsonDataFilter : IRaygunDataFilter
  {
    public bool CanParse(string data)
    {
      if (!string.IsNullOrEmpty(data))
      {
        int index = data.TakeWhile(c => char.IsWhiteSpace(c)).Count();

        if (index < data.Length)
        {
          var firstChar = data.ElementAt(index);
          if (firstChar.Equals('{') || firstChar.Equals('['))
          {
            return true;
          }
        }
      }

      return false;
    }

    public string Filter(string data, IList<string> sensitiveValues)
    {
      throw new NotImplementedException();
    }
  }
}
