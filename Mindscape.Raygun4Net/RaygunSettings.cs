using System;
#if WINRT
using Windows.Storage;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings
  {
    private static readonly RaygunSettings settings = ApplicationData.Current.LocalSettings.Values["RaygunSettings"] as RaygunSettings;

    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";

    public static RaygunSettings Settings
    {
      get
      {
        // If no configuration setting is provided then return the default values
        return settings ?? new RaygunSettings { ApiKey = "", ApiEndpoint = new Uri(DefaultApiEndPoint) };
      }
    }

    public string ApiKey { get; set; }

    public Uri ApiEndpoint { get; set; }

    public bool ThrowOnError { get; set; }
  }
}
#elif WINDOWS_PHONE
namespace Mindscape.Raygun4Net
{
  public class RaygunSettings
  {
    private static readonly RaygunSettings settings = null; //ApplicationData.Current.LocalSettings.Values["RaygunSettings"] as RaygunSettings;

    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";

    public static RaygunSettings Settings
    {
      get
      {
        // If no configuration setting is provided then return the default values
        return settings ?? new RaygunSettings { ApiKey = "", ApiEndpoint = new Uri(DefaultApiEndPoint) };
      }
    }

    public string ApiKey { get; set; }

    public Uri ApiEndpoint { get; set; }
  }
}
#elif ANDROID || IOS
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
        // If no configuration setting is provided then return the default values
        return settings ?? (settings = new RaygunSettings { ApiKey = "", ApiEndpoint = new Uri(DefaultApiEndPoint) });
      }
    }

    public string ApiKey { get; set; }

    public Uri ApiEndpoint { get; set; }
  }
}
#else
using System.Configuration;
namespace Mindscape.Raygun4Net
{
  public class RaygunSettings : ConfigurationSection
  {
    private static readonly RaygunSettings settings = ConfigurationManager.GetSection("RaygunSettings") as RaygunSettings;

    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";

    public static RaygunSettings Settings
    {
      get
      {
        // If no configuration setting is provided then return the default values
        return settings ?? new RaygunSettings { ApiKey = "", ApiEndpoint = new Uri(DefaultApiEndPoint), MediumTrust = false };
      }
    }

    [ConfigurationProperty("apikey", IsRequired = true)]
    [StringValidator]
    public string ApiKey
    {
      get { return (string)this["apikey"]; }
      set { this["apikey"] = value; }
    }

    [ConfigurationProperty("endpoint", IsRequired = false, DefaultValue = DefaultApiEndPoint)]
    public Uri ApiEndpoint
    {
      get { return (Uri)this["endpoint"]; }
      set { this["endpoint"] = value; }
    }

    [ConfigurationProperty("mediumTrust", IsRequired = false, DefaultValue = false)]
    public bool MediumTrust
    {
      get { return (bool)this["mediumTrust"]; }
      set { this["mediumTrust"] = value; }
    }

    [ConfigurationProperty("throwOnError", IsRequired = false, DefaultValue = false)]
    public bool ThrowOnError
    {
      get { return (bool)this["throwOnError"]; }
      set { this["throwOnError"] = value; }
    } 
  }
}
#endif