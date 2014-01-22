using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;

using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;
    private static List<Type> _wrapperExceptions;
    private List<string> _ignoredFormNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
      _wrapperExceptions = new List<Type>();
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
    /// Gets or sets the user identity string.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all error messages sent to the Raygun.io endpoint.
    /// </summary>
    public string ApplicationVersion { get; set; }

    /// <summary>
    /// Sends the exception from an UnhandledException event to Raygun.io, optionally with a list of tags
    /// for identification.
    /// </summary>
    /// <param name="unhandledExceptionEventArgs">The event args from UnhandledException, containing the thrown exception and its message.</param>
    /// <param name="tags">An optional list of strings to identify the message to be transmitted.</param>
    /// <param name="userCustomData">A key-value collection of custom data that is to be sent along with the message.</param>
    public void Send(UnhandledExceptionEventArgs unhandledExceptionEventArgs, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      var exception = unhandledExceptionEventArgs.Exception;
      exception.Data.Add("Message", unhandledExceptionEventArgs.Message);

      Send(BuildMessage(exception, tags, userCustomData));
    }

    /// <summary>
    /// Sends the exception to Raygun.io, optionally with a list of tags for identification.
    /// </summary>
    /// <param name="exception">The exception thrown by the wrapped method.</param>
    /// <param name="tags">A list of string tags relating to the message to identify it.</param>
    /// <param name="userCustomData">A key-value collection of custom data that is to be sent along with the message.</param>
    public void Send(Exception exception, [Optional] IList<string> tags, [Optional] IDictionary userCustomData)
    {
      Send(BuildMessage(exception, tags, userCustomData));
    }

    public async void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey())
      {
        HttpClientHandler handler = new HttpClientHandler {UseDefaultCredentials = true};

        var client = new HttpClient(handler);
        {
          client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("raygun4net-winrt", "1.0.0"));

          HttpContent httpContent = new StringContent(SimpleJson.SerializeObject(raygunMessage));
          httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-raygun-message");
          httpContent.Headers.Add("X-ApiKey", _apiKey);

          try
          {
            await PostMessageAsync(client, httpContent, RaygunSettings.Settings.ApiEndpoint);
          }
          catch (Exception ex)
          {
            System.Diagnostics.Debug.WriteLine("Error Logging Exception to Raygun.io {0}", ex.Message);

            if (RaygunSettings.Settings.ThrowOnError)
            {
              throw;
            }
          }
        }
      }
    }

    private RaygunMessage BuildMessage(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var message = RaygunMessageBuilder.New
          .SetEnvironmentDetails()
          .SetMachineName(NetworkInformation.GetHostNames()[0].DisplayName)
          .SetExceptionDetails(exception)
          .SetClientDetails()
          .SetVersion(ApplicationVersion)
          .SetTags(tags)
          .SetUserCustomData(userCustomData)
          .SetUser(User)
          .Build();

      return message;
    }

#pragma warning disable 1998
    private async Task PostMessageAsync(HttpClient client, HttpContent httpContent, Uri uri)
#pragma warning restore 1998
    {
      HttpResponseMessage response;
      response = client.PostAsync(uri, httpContent).Result;
      client.Dispose();
    }

    public void Wrap(Action func, [Optional] List<string> tags)
    {
      try
      {
        func();
      }
      catch (Exception ex)
      {
        Send(ex, tags);
        throw;
      }
    }

    public TResult Wrap<TResult>(Func<TResult> func, [Optional] List<string> tags)
    {
      try
      {
        return func();
      }
      catch (Exception ex)
      {
        Send(ex, tags);
        throw;
      }
    }
  }
}
