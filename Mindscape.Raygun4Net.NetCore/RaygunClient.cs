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

  // ReSharper disable MemberCanBeProtected.Global
  // ReSharper disable SuggestBaseTypeForParameterInConstructor
  // ReSharper disable UnusedMember.Global
  public RaygunClient(RaygunSettings settings) : base(settings)
  {
  }

  public RaygunClient(RaygunSettings settings, HttpClient httpClient) : base(settings, httpClient)
  {
  }

  public RaygunClient(RaygunSettings settings, IRaygunUserProvider userProvider) : base(settings, userProvider)
  {
  }

  public RaygunClient(RaygunSettings settings, HttpClient httpClient, IRaygunUserProvider userProvider) : base(settings, httpClient, userProvider)
  {
  }

  // ReSharper restore MemberCanBeProtected.Global
  // ReSharper restore SuggestBaseTypeForParameterInConstructor
  // ReSharper restore UnusedMember.Global
}