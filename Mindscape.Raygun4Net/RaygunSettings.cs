using System;
using System.Configuration;
using System.Linq;
using Mindscape.Raygun4Net.Breadcrumbs;
using Mindscape.Raygun4Net.Logging;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings : ConfigurationSection
  {
    private static RaygunSettings settings = ConfigurationManager.GetSection("RaygunSettings") as RaygunSettings ?? new RaygunSettings();

    private const string DefaultApiEndPoint = "https://api.raygun.com/entries";

    public const int MaxCrashReportsStoredOfflineHardLimit = 64;

    public static RaygunSettings Settings
    {
      get { return settings; }
#if DEBUG
      internal set => settings = value; //Needed to be able to reset, after some unit tests pollute this global object.
#endif
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

    [ConfigurationProperty("excludeHttpStatusCodes", IsRequired = false, DefaultValue = "")]
    [RegexStringValidator(@"^(\d+(,\s?\d+)*)?$")]
    public string ExcludeHttpStatusCodesList
    {
      get { return (string)this["excludeHttpStatusCodes"]; }
      set { this["excludeHttpStatusCodes"] = value; }
    }

    public int[] ExcludedStatusCodes
    {
      get { return string.IsNullOrEmpty(ExcludeHttpStatusCodesList) ? new int[0] : ExcludeHttpStatusCodesList.Split(',').Select(int.Parse).ToArray(); }
    }

    [ConfigurationProperty("excludeErrorsFromLocal", IsRequired = false, DefaultValue = false)]
    public bool ExcludeErrorsFromLocal
    {
      get { return (bool)this["excludeErrorsFromLocal"]; }
      set { this["excludeErrorsFromLocal"] = value; }
    }

    [ConfigurationProperty("ignoreSensitiveFieldNames", IsRequired = false, DefaultValue = "")]
    public string IgnoreSensitiveFieldNames
    {
      get { return (string)this["ignoreSensitiveFieldNames"]; }
      set { this["ignoreSensitiveFieldNames"] = value; }
    }

    [ConfigurationProperty("ignoreQueryParameterNames", IsRequired = false, DefaultValue = "")]
    public string IgnoreQueryParameterNames
    {
      get { return (string)this["ignoreQueryParameterNames"]; }
      set { this["ignoreQueryParameterNames"] = value; }
    }

    [ConfigurationProperty("ignoreFormFieldNames", IsRequired = false, DefaultValue = "")]
    public string IgnoreFormFieldNames
    {
      get { return (string)this["ignoreFormFieldNames"]; }
      set { this["ignoreFormFieldNames"] = value; }
    }

    [ConfigurationProperty("ignoreHeaderNames", IsRequired = false, DefaultValue = "")]
    public string IgnoreHeaderNames
    {
      get { return (string)this["ignoreHeaderNames"]; }
      set { this["ignoreHeaderNames"] = value; }
    }

    [ConfigurationProperty("ignoreCookieNames", IsRequired = false, DefaultValue = "")]
    public string IgnoreCookieNames
    {
      get { return (string)this["ignoreCookieNames"]; }
      set { this["ignoreCookieNames"] = value; }
    }

    [ConfigurationProperty("ignoreServerVariableNames", IsRequired = false, DefaultValue = "")]
    public string IgnoreServerVariableNames
    {
      get { return (string)this["ignoreServerVariableNames"]; }
      set { this["ignoreServerVariableNames"] = value; }
    }

    [ConfigurationProperty("isRawDataIgnored", IsRequired = false, DefaultValue = false)]
    public bool IsRawDataIgnored
    {
      get { return (bool)this["isRawDataIgnored"]; }
      set { this["isRawDataIgnored"] = value; }
    }

    [ConfigurationProperty("useXmlRawDataFilter", IsRequired = false, DefaultValue = false)]
    public bool UseXmlRawDataFilter
    {
      get { return (bool)this["useXmlRawDataFilter"]; }
      set { this["useXmlRawDataFilter"] = value; }
    }

    [ConfigurationProperty("useKeyValuePairRawDataFilter", IsRequired = false, DefaultValue = false)]
    public bool UseKeyValuePairRawDataFilter
    {
      get { return (bool)this["useKeyValuePairRawDataFilter"]; }
      set { this["useKeyValuePairRawDataFilter"] = value; }
    }

    [ConfigurationProperty("isRawDataIgnoredWhenFilteringFailed", IsRequired = false, DefaultValue = false)]
    public bool IsRawDataIgnoredWhenFilteringFailed
    {
      get { return (bool)this["isRawDataIgnoredWhenFilteringFailed"]; }
      set { this["isRawDataIgnoredWhenFilteringFailed"] = value; }
    }

    [ConfigurationProperty("isResponseContentIgnored", IsRequired = false, DefaultValue = true)]
    public bool IsResponseContentIgnored
    {
      get { return (bool)this["isResponseContentIgnored"]; }
      set { this["isResponseContentIgnored"] = value; }
    }

    [ConfigurationProperty("applicationVersion", IsRequired = false, DefaultValue = "")]
    public string ApplicationVersion
    {
        get { return (string)this["applicationVersion"]; }
        set { this["applicationVersion"] = value; }
    }

    [ConfigurationProperty("breadcrumbsLevel", IsRequired = false, DefaultValue = "Info")]
    public RaygunBreadcrumbLevel BreadcrumbsLevel
    {
      get { return (RaygunBreadcrumbLevel) this["breadcrumbsLevel"]; }
      set { this["breadcrumbsLevel"] = value; }
    }

    [ConfigurationProperty("breadcrumbsLocationRecordingEnabled", IsRequired = false, DefaultValue = false)]
    public bool BreadcrumbsLocationRecordingEnabled
    {
      get { return (bool)this["breadcrumbsLocationRecordingEnabled"]; }
      set { this["breadcrumbsLocationRecordingEnabled"] = value; }
    }

    [ConfigurationProperty("applicationIdentifier", IsRequired = false, DefaultValue = "")]
    public string ApplicationIdentifier
    {
      get { return (string)this["applicationIdentifier"]; }
      set { this["applicationIdentifier"] = value; }
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
