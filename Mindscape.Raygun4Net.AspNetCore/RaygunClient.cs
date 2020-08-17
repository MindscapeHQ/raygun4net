using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Mindscape.Raygun4Net.AspNetCore.Builders;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.AspNetCore
{
  public class RaygunClient : RaygunClientBase
  {
    protected readonly RaygunRequestMessageOptions _requestMessageOptions = new RaygunRequestMessageOptions();

    private readonly ThreadLocal<HttpContext> _currentHttpContext = new ThreadLocal<HttpContext>(() => null);
    private readonly ThreadLocal<RaygunRequestMessage> _currentRequestMessage = new ThreadLocal<RaygunRequestMessage>(() => null);
    private readonly ThreadLocal<RaygunResponseMessage> _currentResponseMessage = new ThreadLocal<RaygunResponseMessage>(() => null);

    public RaygunClient(string apiKey)
      : this(new RaygunSettings {ApiKey = apiKey})
    {
    }

    public RaygunClient(RaygunSettings settings, HttpContext context = null)
    : base(settings)
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

      if (context != null)
      {
        SetCurrentContext(context);
      }
    }

    RaygunSettings GetSettings()
    {
      return (RaygunSettings) _settings;
    }

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
      get { return _requestMessageOptions.UseKeyValuePairRawDataFilter; }
      set { _requestMessageOptions.UseKeyValuePairRawDataFilter = value; }
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

      RaygunSettings settings = GetSettings();
      if (settings.ExcludedStatusCodes == null)
      {
        return true;
      }

      return !settings.ExcludedStatusCodes.Contains(message.Details.Response.StatusCode);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    /// <param name="userInfo">Information about the user including the identity string.</param>
    public override async Task SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfo)
    {
        if (CanSend(exception))
        {
            // We need to process the Request on the current thread,
            // otherwise it will be disposed while we are using it on the other thread.
            RaygunRequestMessage currentRequestMessage = await BuildRequestMessage();
            RaygunResponseMessage currentResponseMessage = BuildResponseMessage();

            var task = Task.Run(async () =>
            {
                _currentRequestMessage.Value = currentRequestMessage;
                _currentResponseMessage.Value = currentResponseMessage;
                await StripAndSend(exception, tags, userCustomData, userInfo);
            });
            FlagAsSent(exception);
            await task;
        }
    }

    private async Task<RaygunRequestMessage> BuildRequestMessage()
    {
      var message = _currentHttpContext.Value != null ? await RaygunAspNetCoreRequestMessageBuilder.Build(_currentHttpContext.Value, _requestMessageOptions) : null;
      _currentHttpContext.Value = null;
      return message;
    }

    private RaygunResponseMessage BuildResponseMessage()
    {
      var message = _currentHttpContext.Value != null ? RaygunAspNetCoreResponseMessageBuilder.Build(_currentHttpContext.Value) : null;
      _currentHttpContext.Value = null;
      return message;
    }

    public RaygunClient SetCurrentContext(HttpContext request)
    {
      _currentHttpContext.Value = request;
      return this;
    }

    protected override async Task<RaygunMessage> BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData, RaygunIdentifierMessage userInfoMessage)
    {
      var message = RaygunMessageBuilder.New(GetSettings())
        .SetResponseDetails(_currentResponseMessage.Value)
        .SetRequestDetails(_currentRequestMessage.Value)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(userInfoMessage ?? UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Build();

      var customGroupingKey = await OnCustomGroupingKey(exception, message);
      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }

      return message;
    }
  }

}
