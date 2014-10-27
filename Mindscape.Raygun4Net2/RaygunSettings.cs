using System;
using System.Configuration;

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
  }
}
