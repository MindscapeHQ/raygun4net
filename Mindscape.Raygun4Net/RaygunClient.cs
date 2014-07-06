﻿using System.Web;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient : RaygunClientBase
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey) : base(apiKey) {}

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
    }


    protected override IRaygunMessageBuilder BuildMessage()
    {
      return RaygunMessageBuilder.New
        .SetHttpDetails(HttpContext.Current, _requestMessageOptions);
    }
  }
}
