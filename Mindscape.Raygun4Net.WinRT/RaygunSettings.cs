using System;
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
        try
        {
          // The try catch block is to get the tests working
          return settings ?? new RaygunSettings { ApiKey = "", ApiEndpoint = new Uri(DefaultApiEndPoint) };
        }
        catch (Exception)
        {
          return new RaygunSettings { ApiKey = "", ApiEndpoint = new Uri(DefaultApiEndPoint) };
        }
      }
    }

    public string ApiKey { get; set; }

    public Uri ApiEndpoint { get; set; }

    public bool ThrowOnError { get; set; }
  }
}
