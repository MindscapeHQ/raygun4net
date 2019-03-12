using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Filters
{
  public interface IRaygunDataFilter
  {
    bool CanParse(string data);

    string Filter(string data, IList<string> ignoredKeys);
  }
}
