using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;

using System.Web;
using System.Threading;
using System.Reflection;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;
    private static List<Type> _wrapperExceptions;
    private List<string> _ignoredFormNames; 

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
      _wrapperExceptions = new List<Type>();

      _wrapperExceptions.Add(typeof (TargetInvocationException));
      _wrapperExceptions.Add(typeof (HttpUnhandledException));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// Uses the ApiKey specified in the config file.
    /// </summary>
    public RaygunClient()
      : this(RaygunSettings.Settings.ApiKey)
    {
    }

    private bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
        System.Diagnostics.Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
        return false;
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
    /// <param name="wrapperExceptions">An enumerable list of exception types that you want removed and replaced with their inner exception.</param>
    public void AddWrapperExceptions(IEnumerable<Type> wrapperExceptions)
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
    /// Adds a list of keys to ignore when attaching the Form data of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the Form on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    /// <param name="names">An enumerable list of keys (Names) to be stripped from the copy of the Form NameValueCollection when sending to Raygun</param>
    public void IgnoreFormDataNames(IEnumerable<string> names)
    {
      if (_ignoredFormNames == null)
      {
        _ignoredFormNames = new List<string>();
      }

      foreach (string name in names)
      {
        _ignoredFormNames.Add(name);
      }
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously, using the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver</param>
    public void Send(Exception exception)
    {
      exception = StripWrapperExceptions(exception);
      Send(BuildMessage(exception));
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification. This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver</param>
    /// <param name="tags">A list of strings associated with the message</param>
    public void Send(Exception exception, IList<string> tags)
    {
      exception = StripWrapperExceptions(exception);
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      Send(message);
    }

    public void Send(Exception exception, IList<string> tags, string version)
    {
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      message.Details.Version = version;
      Send(message);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// This uses the version number of the originating assembly.
    /// </summary>
    /// <param name="exception">The exception to deliver</param>
    /// <param name="tags">A list of strings associated with the message</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      exception = StripWrapperExceptions(exception);
      var message = BuildMessage(exception);
      message.Details.Tags = tags;      
      message.Details.UserCustomData = userCustomData;
      Send(message);
    }

    /// <summary>
    /// Transmits an exception to Raygun.io synchronously specifying a list of string tags associated
    /// with the message for identification, as well as sending a key-value collection of custom data.
    /// This specifies a custom version identification number.
    /// </summary>
    /// <param name="exception">The exception to deliver</param>
    /// <param name="tags">A list of strings associated with the message</param>
    /// <param name="userCustomData">A key-value collection of custom data that will be added to the payload</param>
    /// <param name="version">A custom version identifiction, associated with a particular build of your project.</param>
    public void Send(Exception exception, IList<string> tags, IDictionary userCustomData, string version)
    {
      exception = StripWrapperExceptions(exception);
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      message.Details.UserCustomData = userCustomData;
      message.Details.Version = version;
      Send(message);
    }

    private static Exception StripWrapperExceptions(Exception exception)
    {
      if (_wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
      {
        return StripWrapperExceptions(exception.InnerException);
      }

      return exception;
    }

    /// <summary>
    /// Asynchronously transmits a message to Raygun.io.
    /// </summary>
    /// <param name="exception"></param>
    public void SendInBackground(Exception exception)
    {
      var message = BuildMessage(exception);

      ThreadPool.QueueUserWorkItem(c => Send(message));
    }

    public void SendInBackground(Exception exception, IList<string> tags)
    {
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      ThreadPool.QueueUserWorkItem(c => Send(message));
    }

    public void SendInBackground(Exception exception, IList<string> tags, string version)
    {
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      message.Details.Version = version;
      ThreadPool.QueueUserWorkItem(c => Send(message));
    }

    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var message = BuildMessage(exception);
      message.Details.UserCustomData = userCustomData;
      message.Details.Tags = tags;
      ThreadPool.QueueUserWorkItem(c => Send(message));
    }

    public void SendInBackground(Exception exception, IList<string> tags, IDictionary userCustomData, string version)
    {
      var message = BuildMessage(exception);
      message.Details.UserCustomData = userCustomData;
      message.Details.Tags = tags;
      message.Details.Version = version;
      ThreadPool.QueueUserWorkItem(c => Send(message));
    }

    public void SendInBackground(RaygunMessage raygunMessage)
    {
      ThreadPool.QueueUserWorkItem(c => Send(raygunMessage));
    }

    internal RaygunMessage BuildMessage(Exception exception)
    {
      var message = RaygunMessageBuilder.New
        .SetHttpDetails(HttpContext.Current, _ignoredFormNames)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion()
        .SetUser(User)
        .Build();
      return message;
    }

    /// <summary>
    /// Posts a RaygunMessage to the Raygun.io api endpoint.
    /// </summary>
    /// <param name="raygunMessage">The RaygunMessage to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        using (var client = new WebClient())
        {
          client.Headers.Add("X-ApiKey", _apiKey);
          client.Encoding = System.Text.Encoding.UTF8;

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
