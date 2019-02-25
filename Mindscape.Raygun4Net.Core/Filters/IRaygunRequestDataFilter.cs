using System;
namespace Mindscape.Raygun4Net.Filters
{
  public interface IRaygunRequestDataFilter
  {
    string Apply(string data);
  }
}
