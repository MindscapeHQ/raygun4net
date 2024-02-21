using System;
using System.Linq;
using System.Net.Http;

namespace Mindscape.Raygun4Net.AspNetCore;

public class RaygunClient : RaygunClientBase
{
  [Obsolete("Please use the RaygunClient(RaygunSettings settings) constructor instead.")]
  public RaygunClient(string apiKey) : base(new RaygunSettings {ApiKey = apiKey})
  {
  }

  public RaygunClient(RaygunSettings settings, HttpClient httpClient = null, IRaygunUserProvider userProvider = null) : base(settings, httpClient, userProvider)
  {
  }

  public RaygunClient(RaygunSettings settings, IRaygunUserProvider userProvider = null) : base(settings, null, userProvider)
  {
  }

  internal Lazy<RaygunSettings> Settings => new(() => (RaygunSettings) _settings);

  protected override bool CanSend(RaygunMessage message)
  {
    if (message?.Details?.Response == null)
    {
      return true;
    }

    var settings = Settings.Value;
    if (settings.ExcludedStatusCodes == null)
    {
      return true;
    }

    return !settings.ExcludedStatusCodes.Contains(message.Details.Response.StatusCode);
  }
}