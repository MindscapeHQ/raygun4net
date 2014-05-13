using System;
using System.Collections.Generic;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings
  {
    private static RaygunSettings settings;
    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";

    public static RaygunSettings Settings
    {
      get
      {
        return settings ?? (settings = new RaygunSettings { ApiEndpoint = new Uri(DefaultApiEndPoint) });
      }
    }

    public Uri ApiEndpoint { get; set; }
  }
}
