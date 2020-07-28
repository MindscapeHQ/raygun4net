using System.Collections.Generic;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Storage
{
  public class RaygunOfflineStorage : IRaygunOfflineStorage
  {
    public bool Store(RaygunMessage message, string apiKey)
    {
      throw new System.NotImplementedException();
    }

    public IList<IRaygunFile> FetchAll(string apiKey)
    {
      throw new System.NotImplementedException();
    }

    public bool Remove(string name, string apiKey)
    {
      throw new System.NotImplementedException();
    }
  }
}