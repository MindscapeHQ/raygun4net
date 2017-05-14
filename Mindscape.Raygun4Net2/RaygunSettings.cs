using System;
using System.Configuration;
using Mindscape.Raygun4Net.Breadcrumbs;

namespace Mindscape.Raygun4Net
{
  public class RaygunSettings : ConfigurationSection
  {
    private static readonly RaygunSettings settings = ConfigurationManager.GetSection("RaygunSettings") as RaygunSettings ?? new RaygunSettings();

    private const string DefaultApiEndPoint = "https://api.raygun.io/entries";

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

    [ConfigurationProperty("excludeHttpStatusCodes", IsRequired = false, DefaultValue = "")]
    [RegexStringValidator(@"^(\d+(,\s?\d+)*)?$")]
    public string ExcludeHttpStatusCodesList
    {
      get { return (string)this["excludeHttpStatusCodes"]; }
      set { this["excludeHttpStatusCodes"] = value; }
    }

    [ConfigurationProperty("excludeErrorsFromLocal", IsRequired = false, DefaultValue = false)]
    public bool ExcludeErrorsFromLocal
    {
      get { return (bool)this["excludeErrorsFromLocal"]; }
      set { this["excludeErrorsFromLocal"] = value; }
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

    [ConfigurationProperty("applicationVersion", IsRequired = false, DefaultValue = "")]
    public string ApplicationVersion
    {
        get { return (string)this["applicationVersion"]; }
        set { this["applicationVersion"] = value; }
    }

    [ConfigurationProperty("throwOnError", IsRequired = false, DefaultValue = false)]
    public bool ThrowOnError
    {
      get { return (bool) this["throwOnError"]; }
      set { this["throwOnError"] = value; }
    }

    [ConfigurationProperty("breadcrumbsLevel", IsRequired = false, DefaultValue = "Info")]
    public BreadcrumbLevel BreadcrumbsLevel
    {
      get { return (BreadcrumbLevel) this["breadcrumbsLevel"]; }
      set { this["breadcrumbsLevel"] = value; }
    }

    [ConfigurationProperty("breadcrumbsLocationRecordingEnabled", IsRequired = false, DefaultValue = false)]
    public bool BreadcrumbsLocationRecordingEnabled
    {
      get { return (bool) this["breadcrumbsLocationRecordingEnabled"]; }
      set { this["breadcrumbsLocationRecordingEnabled"] = value; }
    }
  }
}
