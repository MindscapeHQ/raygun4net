using System;
using System.Linq;
using System.Net.Http;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.AspNetCore;

public class RaygunClient : RaygunClientBase
{
  private readonly RaygunRequestMessageOptions _requestMessageOptions = new();
    
  public RaygunClient(string apiKey)
    : this(new RaygunSettings {ApiKey = apiKey})
  {
  }

  public RaygunClient(RaygunSettings settings, HttpClient httpClient = null)
    : base(settings, httpClient)
  {
    if (settings.IgnoreSensitiveFieldNames != null)
    {
      var ignoredNames = settings.IgnoreSensitiveFieldNames;
      IgnoreSensitiveFieldNames(ignoredNames);
    }

    if (settings.IgnoreQueryParameterNames != null)
    {
      var ignoredNames = settings.IgnoreQueryParameterNames;
      IgnoreQueryParameterNames(ignoredNames);
    }

    if (settings.IgnoreFormFieldNames != null)
    {
      var ignoredNames = settings.IgnoreFormFieldNames;
      IgnoreFormFieldNames(ignoredNames);
    }

    if (settings.IgnoreHeaderNames != null)
    {
      var ignoredNames = settings.IgnoreHeaderNames;
      IgnoreHeaderNames(ignoredNames);
    }

    if (settings.IgnoreCookieNames != null)
    {
      var ignoredNames = settings.IgnoreCookieNames;
      IgnoreCookieNames(ignoredNames);
    }

    if (settings.IgnoreServerVariableNames != null)
    {
      var ignoredNames = settings.IgnoreServerVariableNames;
      IgnoreServerVariableNames(ignoredNames);
    }

    if (!string.IsNullOrEmpty(settings.ApplicationVersion))
    {
      ApplicationVersion = settings.ApplicationVersion;
    }

    IsRawDataIgnored = settings.IsRawDataIgnored;
    IsRawDataIgnoredWhenFilteringFailed = settings.IsRawDataIgnoredWhenFilteringFailed;

    UseXmlRawDataFilter = settings.UseXmlRawDataFilter;
    UseKeyValuePairRawDataFilter = settings.UseKeyValuePairRawDataFilter;
  }

  private Lazy<RaygunSettings> Settings => new(() => (RaygunSettings) _settings);

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
    _requestMessageOptions.AddSensitiveFieldNames(names);
  }

  /// <summary>
  /// Adds a list of keys to remove from the <see cref="RaygunRequestMessage.QueryString" /> property of the <see cref="RaygunRequestMessage" />
  /// </summary>
  /// <param name="names">Keys to be stripped from the <see cref="RaygunRequestMessage.QueryString" /></param>
  public void IgnoreQueryParameterNames(params string[] names)
  {
    _requestMessageOptions.AddQueryParameterNames(names);
  }

  /// <summary>
  /// Adds a list of keys to ignore when attaching the Form data of an HTTP POST request. This allows
  /// you to remove sensitive data from the transmitted copy of the Form on the HttpRequest by specifying the keys you want removed.
  /// This method is only effective in a web context.
  /// </summary>
  /// <param name="names">Keys to be stripped from the copy of the Form NameValueCollection when sending to Raygun.</param>
  public void IgnoreFormFieldNames(params string[] names)
  {
    _requestMessageOptions.AddFormFieldNames(names);
  }

  /// <summary>
  /// Adds a list of keys to ignore when attaching the headers of an HTTP POST request. This allows
  /// you to remove sensitive data from the transmitted copy of the Headers on the HttpRequest by specifying the keys you want removed.
  /// This method is only effective in a web context.
  /// </summary>
  /// <param name="names">Keys to be stripped from the copy of the Headers NameValueCollection when sending to Raygun.</param>
  public void IgnoreHeaderNames(params string[] names)
  {
    _requestMessageOptions.AddHeaderNames(names);
  }

  /// <summary>
  /// Adds a list of keys to ignore when attaching the cookies of an HTTP POST request. This allows
  /// you to remove sensitive data from the transmitted copy of the Cookies on the HttpRequest by specifying the keys you want removed.
  /// This method is only effective in a web context.
  /// </summary>
  /// <param name="names">Keys to be stripped from the copy of the Cookies NameValueCollection when sending to Raygun.</param>
  public void IgnoreCookieNames(params string[] names)
  {
    _requestMessageOptions.AddCookieNames(names);
  }

  /// <summary>
  /// Adds a list of keys to ignore when attaching the server variables of an HTTP POST request. This allows
  /// you to remove sensitive data from the transmitted copy of the ServerVariables on the HttpRequest by specifying the keys you want removed.
  /// This method is only effective in a web context.
  /// </summary>
  /// <param name="names">Keys to be stripped from the copy of the ServerVariables NameValueCollection when sending to Raygun.</param>
  public void IgnoreServerVariableNames(params string[] names)
  {
    _requestMessageOptions.AddServerVariableNames(names);
  }

  /// <summary>
  /// Specifies whether or not RawData from web requests is ignored when sending reports to Raygun.
  /// The default is false which means RawData will be sent to Raygun.
  /// </summary>
  public bool IsRawDataIgnored
  {
    get { return _requestMessageOptions.IsRawDataIgnored; }
    set
    {
      _requestMessageOptions.IsRawDataIgnored = value;
    }
  }

  /// <summary>
  /// Specifies whether or not RawData from web requests is ignored when sensitive values are seen and unable to be removed due to failing to parse the contents.
  /// The default is false which means RawData will not be ignored when filtering fails.
  /// </summary>
  public bool IsRawDataIgnoredWhenFilteringFailed
  {
    get { return _requestMessageOptions.IsRawDataIgnoredWhenFilteringFailed; }
    set { _requestMessageOptions.IsRawDataIgnoredWhenFilteringFailed = value; }
  }

  /// <summary>
  /// Specifies whether or not RawData from web requests is filtered of sensitive values using an XML parser.
  /// </summary>
  /// <value><c>true</c> if use xml raw data filter; otherwise, <c>false</c>.</value>
  public bool UseXmlRawDataFilter
  {
    get { return _requestMessageOptions.UseXmlRawDataFilter; }
    set { _requestMessageOptions.UseXmlRawDataFilter = value; }
  }

  /// <summary>
  /// Specifies whether or not RawData from web requests is filtered of sensitive values using an KeyValuePair parser.
  /// </summary>
  /// <value><c>true</c> if use key pair raw data filter; otherwise, <c>false</c>.</value>
  public bool UseKeyValuePairRawDataFilter
  {
    get => _requestMessageOptions.UseKeyValuePairRawDataFilter;
    set => _requestMessageOptions.UseKeyValuePairRawDataFilter = value;
  }

  /// <summary>
  /// Add an <see cref="IRaygunDataFilter"/> implementation to be used when capturing the raw data
  /// of a HTTP request. This filter will be passed the request raw data and is expected to remove
  /// or replace values whose keys are found in the list supplied to the Filter method.
  /// </summary>
  /// <param name="filter">Custom raw data filter implementation.</param>
  public void AddRawDataFilter(IRaygunDataFilter filter)
  {
    _requestMessageOptions.AddRawDataFilter(filter);
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

  ///// <inheritdoc/>
  // public override async Task SendAsync(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo = null)
  // {
  //   if (CanSend(exception))
  //   {
  //     RaygunRequestMessage currentRequestMessage = await BuildRequestMessage();
  //     RaygunResponseMessage currentResponseMessage = BuildResponseMessage();
  //
  //     //_currentHttpContext.Value = null;
  //
  //     _currentRequestMessage.Value = currentRequestMessage;
  //     _currentResponseMessage.Value = currentResponseMessage;
  //
  //     await StripAndSend(exception, tags, userCustomData, null);
  //     FlagAsSent(exception);
  //   }
  // }

  // /// <summary>
  // /// Asynchronously transmits an exception to Raygun.
  // /// </summary>
  // /// <param name="exception">The exception to deliver.</param>
  // /// <param name="tags">A list of strings associated with the message.</param>
  // /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
  // /// <param name="userInfo">Information about the user including the identity string.</param>
  // public override async Task SendInBackground(Exception exception, IList<string> tags = null, IDictionary userCustomData = null, RaygunIdentifierMessage userInfo = null)
  // {
  //   if (CanSend(exception))
  //   {
  //     // We need to process the Request on the current thread,
  //     // otherwise it will be disposed while we are using it on the other thread.
  //     // BuildRequestMessage relies on ReadFormAsync so we need to await it to ensure it's processed before continuing.
  //     var currentRequestMessage = await BuildRequestMessage();
  //     var currentResponseMessage = BuildResponseMessage();
  //
  //     var exceptions = StripWrapperExceptions(exception);
  //
  //     foreach (var ex in exceptions)
  //     {
  //       if (!_backgroundMessageProcessor.Enqueue(async () => await BuildMessage(ex, tags, userCustomData, userInfo,
  //                                                  builder =>
  //                                                  {
  //                                                    builder.SetResponseDetails(currentResponseMessage);
  //                                                    builder.SetRequestDetails(currentRequestMessage);
  //                                                  })))
  //       {
  //         Debug.WriteLine("Could not add message to background queue. Dropping exception: {0}", ex);
  //       }
  //     }
  //
  //     FlagAsSent(exception);
  //   }
  // }

  // internal async Task<RaygunRequestMessage> BuildRequestMessage()
  // {
  //   return _httpContextAccessor?.HttpContext != null ? await RaygunAspNetCoreRequestMessageBuilder.Build(_httpContextAccessor?.HttpContext, _requestMessageOptions) : null;
  // }
  //
  // internal RaygunResponseMessage BuildResponseMessage()
  // {
  //   return _httpContextAccessor?.HttpContext != null ? RaygunAspNetCoreResponseMessageBuilder.Build(_httpContextAccessor?.HttpContext) : null;
  // }
}