using System;
using System.Net.Http;

namespace Mindscape.Raygun4Net;

public class RaygunClient : RaygunClientBase
{
  [Obsolete("Use the RaygunClient(RaygunSettings) constructor instead")]
  public RaygunClient(string apiKey) : base(new RaygunSettings { ApiKey = apiKey })
  {
  }
        
  [Obsolete("Use the RaygunClient(RaygunSettings, HttpClient) constructor instead")]
  public RaygunClient(string apiKey, HttpClient httpClient) : base(new RaygunSettings { ApiKey = apiKey }, httpClient)
  {
  }
        
  public RaygunClient(RaygunSettings settings, HttpClient httpClient = null, IRaygunUserProvider userProvider = null) : base(settings, httpClient, userProvider)
  {
  }

  public RaygunClient(RaygunSettings settings, IRaygunUserProvider userProvider = null) : base(settings, null, userProvider)
  {
  }
}