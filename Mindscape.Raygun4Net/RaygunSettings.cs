using System;
using System.Configuration;

namespace Mindscape.Raygun4Net
{
  internal class RaygunSettings : ConfigurationSection
  {
    private static readonly RaygunSettings settings = ConfigurationManager.GetSection("RaygunSettings") as RaygunSettings;

    public static RaygunSettings Settings
    {
      get
      {
        return settings;
      }
    }

    [ConfigurationProperty("apikey", IsRequired = true)]
    [StringValidator]
    public string ApiKey
    {
      get { return (string)this["apikey"]; }
      set { this["apikey"] = value; }
    }

    [ConfigurationProperty("endpoint", IsRequired = false, DefaultValue = "https://api.raygun.io/entries")]
    public Uri ApiEndpoint
    {
      get { return (Uri)this["endpoint"]; }
      set { this["endpoint"] = value; }
    }
  }
}