using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Filters
{
  public interface IRaygunDataFilter
  {
    /// <summary>
    /// Returns whether or not this filter will be able to parse the following data.
    /// This method is called to determine if the following Filter method will be called.
    /// </summary>
    /// <returns><c>true</c>, if the data is parsable by the implemented class, <c>false</c> otherwise.</returns>
    /// <param name="data">Data.</param>
    bool CanParse(string data);

    /// <summary>
    /// Filter the specified data by checking for the following keys whose values will be removed.
    /// </summary>
    /// <returns>The filter.</returns>
    /// <param name="data">Data.</param>
    /// <param name="ignoredKeys">Keys whose values should be removed.</param>
    string Filter(string data, IList<string> ignoredKeys);
  }
}
