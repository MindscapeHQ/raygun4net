using System;
using System.Linq;
using System.Net.Http;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.AspNetCore;

public class RaygunClient : RaygunClientBase
{
  public RaygunClient(string apiKey)
    : this(new RaygunSettings {ApiKey = apiKey})
  {
  }

  public RaygunClient(RaygunSettings settings, HttpClient httpClient = null)
    : base(settings, httpClient)
  {
    if (!string.IsNullOrEmpty(settings.ApplicationVersion))
    {
      ApplicationVersion = settings.ApplicationVersion;
    }

    IsRawDataIgnored = settings.IsRawDataIgnored;
    IsRawDataIgnoredWhenFilteringFailed = settings.IsRawDataIgnoredWhenFilteringFailed;

    UseXmlRawDataFilter = settings.UseXmlRawDataFilter;
    UseKeyValuePairRawDataFilter = settings.UseKeyValuePairRawDataFilter;
  }

  internal Lazy<RaygunSettings> Settings => new(() => (RaygunSettings) _settings);

  /// <summary>
  /// Adds a list of keys to remove from the following sections of the <see cref="RaygunRequestMessage" />
  /// <see cref="RaygunRequestMessage.Headers" />
  /// <see cref="RaygunRequestMessage.QueryString" />
  /// <see cref="RaygunRequestMessage.Cookies" />
  /// <see cref="RaygunRequestMessage.Form" />
  /// <see cref="RaygunRequestMessage.RawData" />
  /// </summary>
  /// <param name="names">Keys to be stripped from the <see cref="RaygunRequestMessage" />.</param>
  public void IgnoreSensitiveFieldNames(params string[] names)
  {
    Settings.Value.IgnoreSensitiveFieldNames.AddRange(names);
  }

  /// <summary>
  /// Adds a list of keys to remove from the <see cref="RaygunRequestMessage.QueryString" /> property of the <see cref="RaygunRequestMessage" />
  /// </summary>
  /// <param name="names">Keys to be stripped from the <see cref="RaygunRequestMessage.QueryString" /></param>
  public void IgnoreQueryParameterNames(params string[] names)
  {
    Settings.Value.IgnoreQueryParameterNames.AddRange(names);
  }

  /// <summary>
  /// Adds a list of keys to ignore when attaching the Form data of an HTTP POST request. This allows
  /// you to remove sensitive data from the transmitted copy of the Form on the HttpRequest by specifying the keys you want removed.
  /// This method is only effective in a web context.
  /// </summary>
  /// <param name="names">Keys to be stripped from the copy of the Form NameValueCollection when sending to Raygun.</param>
  public void IgnoreFormFieldNames(params string[] names)
  {
    Settings.Value.IgnoreFormFieldNames.AddRange(names);
  }

  /// <summary>
  /// Adds a list of keys to ignore when attaching the headers of an HTTP POST request. This allows
  /// you to remove sensitive data from the transmitted copy of the Headers on the HttpRequest by specifying the keys you want removed.
  /// This method is only effective in a web context.
  /// </summary>
  /// <param name="names">Keys to be stripped from the copy of the Headers NameValueCollection when sending to Raygun.</param>
  public void IgnoreHeaderNames(params string[] names)
  {
    Settings.Value.IgnoreHeaderNames.AddRange(names);
  }

  /// <summary>
  /// Adds a list of keys to ignore when attaching the cookies of an HTTP POST request. This allows
  /// you to remove sensitive data from the transmitted copy of the Cookies on the HttpRequest by specifying the keys you want removed.
  /// This method is only effective in a web context.
  /// </summary>
  /// <param name="names">Keys to be stripped from the copy of the Cookies NameValueCollection when sending to Raygun.</param>
  public void IgnoreCookieNames(params string[] names)
  {
    Settings.Value.IgnoreCookieNames.AddRange(names);
  }

  /// <summary>
  /// Adds a list of keys to ignore when attaching the server variables of an HTTP POST request. This allows
  /// you to remove sensitive data from the transmitted copy of the ServerVariables on the HttpRequest by specifying the keys you want removed.
  /// This method is only effective in a web context.
  /// </summary>
  /// <param name="names">Keys to be stripped from the copy of the ServerVariables NameValueCollection when sending to Raygun.</param>
  public void IgnoreServerVariableNames(params string[] names)
  {
    Settings.Value.IgnoreServerVariableNames.AddRange(names);
  }

  /// <summary>
  /// Specifies whether or not RawData from web requests is ignored when sending reports to Raygun.
  /// The default is false which means RawData will be sent to Raygun.
  /// </summary>
  public bool IsRawDataIgnored
  {
    get => Settings.Value.IsRawDataIgnored;
    set => Settings.Value.IsRawDataIgnored = value;
  }

  /// <summary>
  /// Specifies whether or not RawData from web requests is ignored when sensitive values are seen and unable to be removed due to failing to parse the contents.
  /// The default is false which means RawData will not be ignored when filtering fails.
  /// </summary>
  public bool IsRawDataIgnoredWhenFilteringFailed
  {
    get => Settings.Value.IsRawDataIgnoredWhenFilteringFailed;
    set => Settings.Value.IsRawDataIgnoredWhenFilteringFailed = value;
  }

  /// <summary>
  /// Specifies whether or not RawData from web requests is filtered of sensitive values using an XML parser.
  /// </summary>
  /// <value><c>true</c> if use xml raw data filter; otherwise, <c>false</c>.</value>
  public bool UseXmlRawDataFilter
  {
    get => Settings.Value.UseXmlRawDataFilter;
    set => Settings.Value.UseXmlRawDataFilter = value;
  }

  /// <summary>
  /// Specifies whether or not RawData from web requests is filtered of sensitive values using an KeyValuePair parser.
  /// </summary>
  /// <value><c>true</c> if use key pair raw data filter; otherwise, <c>false</c>.</value>
  public bool UseKeyValuePairRawDataFilter
  {
    get => Settings.Value.UseKeyValuePairRawDataFilter;
    set => Settings.Value.UseKeyValuePairRawDataFilter = value;
  }

  /// <summary>
  /// Add an <see cref="IRaygunDataFilter"/> implementation to be used when capturing the raw data
  /// of a HTTP request. This filter will be passed the request raw data and is expected to remove
  /// or replace values whose keys are found in the list supplied to the Filter method.
  /// </summary>
  /// <param name="filter">Custom raw data filter implementation.</param>
  public void AddRawDataFilter(IRaygunDataFilter filter)
  {
    Settings.Value.RawDataFilters.Add(filter);
  }

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