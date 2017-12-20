using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using Mindscape.Raygun4Net.WebApi.Builders;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Messages;
using System.Net;
using System.Collections;
using System.Linq;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiClient : RaygunClientBase
  {
    private readonly string _apiKey;
    protected readonly RaygunRequestMessageOptions _requestMessageOptions = new RaygunRequestMessageOptions();
    private readonly List<Type> _wrapperExceptions = new List<Type>();

    private readonly ThreadLocal<HttpRequestMessage> _currentWebRequest = new ThreadLocal<HttpRequestMessage>(() => null);
    private readonly ThreadLocal<RaygunRequestMessage> _currentRequestMessage = new ThreadLocal<RaygunRequestMessage>(() => null);

    private static RaygunWebApiExceptionFilter _exceptionFilter;
    private static RaygunWebApiActionFilter _actionFilter;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClientBase" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunWebApiClient(string apiKey)
    {
      _apiKey = apiKey;

      _wrapperExceptions.Add(typeof(TargetInvocationException));

      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreFormFieldNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreFormFieldNames.Split(',');
        IgnoreFormFieldNames(ignoredNames);
      }
      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreHeaderNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreHeaderNames.Split(',');
        IgnoreHeaderNames(ignoredNames);
      }
      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreCookieNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreCookieNames.Split(',');
        IgnoreCookieNames(ignoredNames);
      }
      if (!string.IsNullOrEmpty(RaygunSettings.Settings.IgnoreServerVariableNames))
      {
        var ignoredNames = RaygunSettings.Settings.IgnoreServerVariableNames.Split(',');
        IgnoreServerVariableNames(ignoredNames);
      }
      if (!string.IsNullOrEmpty(RaygunSettings.Settings.ApplicationVersion))
      {
        ApplicationVersion = RaygunSettings.Settings.ApplicationVersion;
      }
      IsRawDataIgnored = RaygunSettings.Settings.IsRawDataIgnored;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClientBase" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunWebApiClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
    }

    /// <summary>
    /// Causes Raygun4Net to listen for exceptions.
    /// </summary>
    /// <param name="config">The HttpConfiguration to attach to.</param>
    public static void Attach(HttpConfiguration config)
    {
      var entryAssembly = Assembly.GetCallingAssembly();
      string version = entryAssembly.GetName().Version.ToString();
      AttachInternal(config, null, version);
    }

    /// <summary>
    /// Causes Raygun4Net to listen for exceptions.
    /// </summary>
    /// <param name="config">The HttpConfiguration to attach to.</param>
    /// <param name="generateRaygunClient">An optional function to provide a custom RaygunWebApiClient instance to use for reporting exceptions.</param>
    public static void Attach(HttpConfiguration config, Func<RaygunWebApiClient> generateRaygunClient)
    {
      var entryAssembly = Assembly.GetCallingAssembly();
      string version = entryAssembly.GetName().Version.ToString();
      if (generateRaygunClient != null)
      {
        AttachInternal(config, message => generateRaygunClient(), version);
      }
      else
      {
        AttachInternal(config, null, version);
      }
    }

    /// <summary>
    /// Causes Raygun4Net to listen for exceptions.
    /// </summary>
    /// <param name="config">The HttpConfiguration to attach to.</param>
    /// <param name="generateRaygunClient">
    /// An optional function to provide a custom RaygunWebApiClient instance to use for reporting exceptions.
    /// The HttpRequestMessage parameter to this function might be null if there is no request in the context of the
    /// failure we are currently handling.
    /// </param>
    public static void Attach(HttpConfiguration config, Func<HttpRequestMessage, RaygunWebApiClient> generateRaygunClient)
    {
      var entryAssembly = Assembly.GetCallingAssembly();
      string version = entryAssembly.GetName().Version.ToString();
      AttachInternal(config, generateRaygunClient, version);
    }

    private static void AttachInternal(HttpConfiguration config, Func<HttpRequestMessage, RaygunWebApiClient> generateRaygunClientWithMessage, string applicationVersionFromAttach)
    {
      Detach(config);

      string applicationVersion;
      if (!string.IsNullOrEmpty(RaygunSettings.Settings.ApplicationVersion))
      {
        applicationVersion = RaygunSettings.Settings.ApplicationVersion;
      }
      else
      {
        applicationVersion = applicationVersionFromAttach;
      }

      var owinAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName.StartsWith("Owin"));
      if (owinAssembly == null && !RaygunSettings.Settings.IsRawDataIgnored)
      {
        config.MessageHandlers.Add(new RaygunWebApiDelegatingHandler());
      }

      var clientCreator = new RaygunWebApiClientProvider(generateRaygunClientWithMessage, applicationVersion);

      config.Services.Add(typeof(IExceptionLogger), new RaygunWebApiExceptionLogger(clientCreator));

      _exceptionFilter = new RaygunWebApiExceptionFilter(clientCreator);
      config.Filters.Add(_exceptionFilter);

      _actionFilter = new RaygunWebApiActionFilter(clientCreator);
      config.Filters.Add(_actionFilter);

      var concreteActivator = config.Services.GetHttpControllerActivator();
      config.Services.Replace(typeof(IHttpControllerActivator), new RaygunWebApiControllerActivator(concreteActivator, clientCreator));

      var concreteControllerSelector = config.Services.GetHttpControllerSelector() ?? new DefaultHttpControllerSelector(config);
      config.Services.Replace(typeof(IHttpControllerSelector), new RaygunWebApiControllerSelector(concreteControllerSelector, clientCreator));

      var concreteActionSelector = config.Services.GetActionSelector() ?? new ApiControllerActionSelector();
      config.Services.Replace(typeof(IHttpActionSelector), new RaygunWebApiActionSelector(concreteActionSelector, clientCreator));
    }

    /// <summary>
    /// Causes Raygun4Net to stop listening for exceptions.
    /// </summary>
    /// <param name="config">The HttpConfiguration to detach from.</param>
    public static void Detach(HttpConfiguration config)
    {
      if (_exceptionFilter != null)
      {
        int exceptionLoggerIndex = config.Services.FindIndex(typeof(IExceptionLogger), (o) => o is RaygunWebApiExceptionLogger);
        if (exceptionLoggerIndex != -1)
        {
          config.Services.RemoveAt(typeof(IExceptionLogger), exceptionLoggerIndex);
        }

        config.Filters.Remove(_exceptionFilter);
        config.Filters.Remove(_actionFilter);

        RaygunWebApiControllerActivator controllerActivator = config.Services.GetHttpControllerActivator() as RaygunWebApiControllerActivator;
        if (controllerActivator != null)
        {
          config.Services.Replace(typeof(IHttpControllerActivator), controllerActivator.ConcreteActivator);
        }

        RaygunWebApiControllerSelector controllerSelector = config.Services.GetHttpControllerSelector() as RaygunWebApiControllerSelector;
        if (controllerSelector != null)
        {
          config.Services.Replace(typeof(IHttpControllerSelector), controllerSelector.ConcreteSelector);
        }

        RaygunWebApiActionSelector actionSelector = config.Services.GetActionSelector() as RaygunWebApiActionSelector;
        if (actionSelector != null)
        {
          config.Services.Replace(typeof(IHttpActionSelector), actionSelector.ConcreteSelector);
        }

        _exceptionFilter = null;
        _actionFilter = null;
      }
    }

    protected bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
        System.Diagnostics.Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
        return false;
      }
      return true;
    }

    /// <summary>
    /// Gets or sets the username/password credentials which are used to authenticate with the system default Proxy server, if one is set
    /// and requires credentials.
    /// </summary>
    public ICredentials ProxyCredentials { get; set; }

    protected override bool CanSend(Exception exception)
    {
      if (RaygunSettings.Settings.ExcludeErrorsFromLocal && _currentWebRequest.Value != null && _currentWebRequest.Value.IsLocal())
      {
        return false;
      }
      return base.CanSend(exception);
    }

    protected bool CanSend(RaygunMessage message)
    {
      if (message != null && message.Details != null && message.Details.Response != null)
      {
        return !RaygunSettings.Settings.ExcludedStatusCodes.Contains(message.Details.Response.StatusCode);
      }
      return true;
    }

    /// <summary>
    /// Adds a list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// This can be used when a wrapper exception, e.g. TargetInvocationException or HttpUnhandledException,
    /// contains the actual exception as the InnerException. The message and stack trace of the inner exception will then
    /// be used by Raygun for grouping and display. The above two do not need to be added manually,
    /// but if you have other wrapper exceptions that you want stripped you can pass them in here.
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that you want removed and replaced with their inner exception.</param>
    public void AddWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (Type wrapper in wrapperExceptions)
      {
        if (!_wrapperExceptions.Contains(wrapper))
        {
          _wrapperExceptions.Add(wrapper);
        }
      }
    }

    /// <summary>
    /// Specifies types of wrapper exceptions that Raygun should send rather than stripping out and sending the inner exception.
    /// This can be used to remove the default wrapper exceptions (TargetInvocationException and HttpUnhandledException).
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that should no longer be stripped away.</param>
    public void RemoveWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (Type wrapper in wrapperExceptions)
      {
        _wrapperExceptions.Remove(wrapper);
      }
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
    /// Specifies whether or not RawData from web requests is ignored when sending reports to Raygun.io.
    /// The default is false which means RawData will be sent to Raygun.io.
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
    /// Transmits an exception to Raygun.io synchronously, using the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public override void Send(Exception exception)
    {
      Send(exception, null, (IDictionary)null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification. This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void Send(Exception exception, IList<string> tags)
    {
      Send(exception, tags, (IDictionary)null);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      if (CanSend(exception))
      {
        _currentRequestMessage.Value = BuildRequestMessage();

        StripAndSend(exception, tags, userCustomData);
        FlagAsSent(exception);
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    public void SendInBackground(Exception exception)
    {
      SendInBackground(exception, null, (IDictionary)null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    public void SendInBackground(Exception exception, IList<string> tags)
    {
      SendInBackground(exception, tags, (IDictionary)null);
    }

    /// <summary>
    /// Asynchronously transmits an exception to Raygun.io.
    /// </summary>
    /// <param name="exception">The exception to deliver.</param>
    /// <param name="tags">A list of strings associated with the message.</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload.</param>
    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      if (CanSend(exception))
      {
        // We need to process the HttpRequestMessage on the current thread,
        // otherwise it will be disposed while we are using it on the other thread.
        RaygunRequestMessage currentRequestMessage = BuildRequestMessage();
        DateTime currentTime = DateTime.UtcNow;

        ThreadPool.QueueUserWorkItem(c => {
          _currentRequestMessage.Value = currentRequestMessage;
          StripAndSend(exception, tags, userCustomData, currentTime);
        });
        FlagAsSent(exception);
      }
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void SendInBackground(RaygunMessage raygunMessage)
    {
      ThreadPool.QueueUserWorkItem(c => Send(raygunMessage));
    }

    internal void FlagExceptionAsSent(Exception exception)
    {
      base.FlagAsSent(exception);
    }

    private RaygunRequestMessage BuildRequestMessage()
    {
      var message = _currentWebRequest.Value != null ? RaygunWebApiRequestMessageBuilder.Build(_currentWebRequest.Value, _requestMessageOptions) : null;
      _currentWebRequest.Value = null;
      return message;
    }

    public RaygunWebApiClient SetCurrentHttpRequest(HttpRequestMessage request)
    {
      _currentWebRequest.Value = request;
      return this;
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      return BuildMessage(exception, tags, userCustomData, null);
    }

    protected RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData, DateTime? currentTime = null)
    {
      var message = RaygunWebApiMessageBuilder.New
        .SetHttpDetails(_currentRequestMessage.Value)
        .SetTimeStamp(currentTime)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion(ApplicationVersion)
        .SetTags(tags)
        .SetUserCustomData(userCustomData)
        .SetUser(UserInfo ?? (!String.IsNullOrEmpty(User) ? new RaygunIdentifierMessage(User) : null))
        .Build();
      var customGroupingKey = OnCustomGroupingKey(exception, message);
      if (string.IsNullOrEmpty(customGroupingKey) == false)
      {
        message.Details.GroupingKey = customGroupingKey;
      }
      return message;
    }

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData, DateTime? currentTime = null)
    {
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        Send(BuildMessage(e, tags, userCustomData, currentTime));
      }
    }

    protected IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && (exception.InnerException != null || exception is ReflectionTypeLoadException)))
      {
        AggregateException aggregate = exception as AggregateException;
        if (aggregate != null)
        {
          foreach (Exception e in aggregate.InnerExceptions)
          {
            foreach (Exception ex in StripWrapperExceptions(e))
            {
              yield return ex;
            }
          }
        }
        else
        {
          ReflectionTypeLoadException rtle = exception as ReflectionTypeLoadException;
          if (rtle != null)
          {
            int index = 0;
            foreach (Exception e in rtle.LoaderExceptions)
            {
              try
              {
                e.Data["Type"] = rtle.Types[index];
              }
              catch { }
              foreach (Exception ex in StripWrapperExceptions(e))
              {
                yield return ex;
              }
              index++;
            }
          }
          else
          {
            foreach (Exception e in StripWrapperExceptions(exception.InnerException))
            {
              yield return e;
            }
          }
        }
      }
      else
      {
        yield return exception;
      }
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public override void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        bool canSend = OnSendingMessage(raygunMessage) && CanSend(raygunMessage);
        if (canSend)
        {
          using (var client = new WebClient())
          {
            client.Headers.Add("X-ApiKey", _apiKey);
            client.Headers.Add("content-type", "application/json; charset=utf-8");
            client.Encoding = System.Text.Encoding.UTF8;

            if (WebRequest.DefaultWebProxy != null)
            {
              Uri proxyUri = WebRequest.DefaultWebProxy.GetProxy(new Uri(RaygunSettings.Settings.ApiEndpoint.ToString()));

              if (proxyUri != null && proxyUri.AbsoluteUri != RaygunSettings.Settings.ApiEndpoint.ToString())
              {
                client.Proxy = new WebProxy(proxyUri, false);

                if (ProxyCredentials == null)
                {
                  client.UseDefaultCredentials = true;
                  client.Proxy.Credentials = CredentialCache.DefaultCredentials;
                }
                else
                {
                  client.UseDefaultCredentials = false;
                  client.Proxy.Credentials = ProxyCredentials;
                }
              }
            }

            try
            {
              var message = SimpleJson.SerializeObject(raygunMessage);
              client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
            }
            catch (Exception ex)
            {
              System.Diagnostics.Trace.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));

              if (RaygunSettings.Settings.ThrowOnError)
              {
                throw;
              }
            }
          }
        }
      }
    }
  }
}