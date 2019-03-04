using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Filters
{
  public interface IRaygunDataFilter
  {
    bool CanParse(string data); // Basic check 

    string Filter(string data, IList<string> ignoredKeys);
  }
}
