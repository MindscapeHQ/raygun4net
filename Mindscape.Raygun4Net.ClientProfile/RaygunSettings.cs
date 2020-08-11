using System;
using System.Configuration;
using System.Linq;
using Mindscape.Raygun4Net.Logging;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings : ConfigurationSection
  {
    private static readonly RaygunSettings settings = ConfigurationManager.GetSection("RaygunSettings") as RaygunSettings ?? new RaygunSettings();

    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";

    public const int MaxCrashReportsStoredOfflineHardLimit = 64;

    public static RaygunSettings Settings
    {
      get { return settings; }
    }

    [ConfigurationProperty("apikey", IsRequired = true, DefaultValue = "")]
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

    [ConfigurationProperty("applicationVersion", IsRequired = false, DefaultValue = "")]
    public string ApplicationVersion
    {
        get { return (string)this["applicationVersion"]; }
        set { this["applicationVersion"] = value; }
    }

    /// <summary>
    /// Gets or sets the max crash reports stored on the device.
    /// There is a hard upper limit of 64 reports.
    /// </summary>
    /// <value>The max crash reports stored on device.</value>
    [ConfigurationProperty("maxCrashReportsStoredOffline", IsRequired = false, DefaultValue = MaxCrashReportsStoredOfflineHardLimit)]
    public int MaxCrashReportsStoredOffline
    {
      get { return (int)this["maxCrashReportsStoredOffline"]; }
      set { this["maxCrashReportsStoredOffline"] = value; }
    }

    /// <summary>
    /// Allows for crash reports to be stored to local storage when there is no available network connection.
    /// </summary>
    /// <value><c>true</c> if allowing crash reports to be stored offline; otherwise, <c>false</c>.</value>
    [ConfigurationProperty("crashReportingOfflineStorageEnabled", IsRequired = false, DefaultValue = true)]
    public bool CrashReportingOfflineStorageEnabled
    {
      get { return (bool)this["crashReportingOfflineStorageEnabled"]; }
      set { this["crashReportingOfflineStorageEnabled"] = value; }
    }

    /// <summary>
    /// Gets or sets the log level controlling the amount of information printed to system consoles.
    /// Setting the level to <see cref="RaygunLogLevel.Verbose"/> will print the raw Crash Reporting being
    /// posted to the API endpoints.
    /// </summary>
    /// <value>The log level.</value>
    [ConfigurationProperty("logLevel", IsRequired = false, DefaultValue = RaygunLogLevel.Warning)]
    public RaygunLogLevel LogLevel
    {
      get { return (RaygunLogLevel)this["logLevel"]; }
      set { this["logLevel"] = value; }
    }

    /// <summary>
    /// Return false.
    /// </summary>
    /// <returns>False</returns>
    public override bool IsReadOnly()
    {
      return false;
    }
  }
}
