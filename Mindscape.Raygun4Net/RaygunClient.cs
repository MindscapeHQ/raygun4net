using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using Mindscape.Raygun4Net.Messages;
#if !WINRT
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif
#if WINRT
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
#else
using System.Web;
using System.Threading;
#endif

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;
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

#if WINRT
    /// <summary>
    /// Sends the exception from an UnhandledException event to Raygun.io, optionally with a list of tags
    /// for identification.
    /// </summary>
    /// <param name="unhandledExceptionEventArgs">The event args from UnhandledException, containing the thrown exception and its message.</param>
    /// <param name="tags">An optional list of strings to identify the message to be transmitted.</param>
    public void Send(UnhandledExceptionEventArgs unhandledExceptionEventArgs, [Optional] List<string> tags)
    {
      if (ValidateApiKey())
      {
        var exception = unhandledExceptionEventArgs.Exception;
        exception.Data.Add("Message", unhandledExceptionEventArgs.Message);

        Send(CreateMessage(exception, tags));
      }
    }

    /// <summary>
    /// To be called by Wrap() - little point in allowing users to send exceptions in WinRT
    /// as the object contains little useful information besides the exception name and description
    /// </summary>
    /// <param name="exception">The exception thrown by the wrapped method</param>
    /// <param name="tags">A list of string tags relating to the message to identify it</param>
    private void Send(Exception exception, [Optional] List<string> tags)
    {
      if (ValidateApiKey())
      {
        Send(CreateMessage(exception, tags));
      }
    }

    public async void Send(RaygunMessage raygunMessage)
    {
      HttpClientHandler handler = new HttpClientHandler();
      handler.UseDefaultCredentials = true;

      var client = new HttpClient(handler);
      {
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("raygun4net-winrt", "1.0.0"));

        HttpContent httpContent = new StringContent(JObject.FromObject(raygunMessage, new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Ignore }).ToString());
        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-raygun-message");
        httpContent.Headers.Add("X-ApiKey", _apiKey);

        try
        {
          await PostMessageAsync(client, httpContent, RaygunSettings.Settings.ApiEndpoint);
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));
        }
      }
    }

    private RaygunMessage CreateMessage(Exception exception, [Optional] List<string> tags)
    {
      var message = RaygunMessageBuilder.New
          .SetEnvironmentDetails()
          .SetMachineName(NetworkInformation.GetHostNames()[0].DisplayName)
          .SetExceptionDetails(exception)
          .SetClientDetails()
          .SetVersion()
          .Build();

      if (tags != null)
      {
        message.Details.Tags = tags;
      }
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
        Send(ex);
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
        Send(ex);
        throw;
      }
    }
#else    
    public void Send(Exception exception)
    {      
      Send(BuildMessage(exception));
    }

    public void Send(Exception exception, List<string> tags)
    {
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      Send(message);
    }

    public void Send(Exception exception, List<string> tags, string version)
    {
      var message = BuildMessage(exception);
      message.Details.Tags = tags;
      message.Details.Version = version;
      Send(message);
    }    

    public void SendInBackground(Exception exception)
    {
      var message = BuildMessage(exception);

      ThreadPool.QueueUserWorkItem(c => Send(message));
    }

    internal RaygunMessage BuildMessage(Exception exception)
    {
      var message = RaygunMessageBuilder.New
        .SetHttpDetails(HttpContext.Current)
        .SetEnvironmentDetails()
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .SetVersion()
        .Build();      
      return message;
    }    

    public void Send(RaygunMessage raygunMessage)
    {
      if (ValidateApiKey()) 
      {
        var client = new WebClient();

        client.UploadStringCompleted += (o,e) => 
        {
          if(e.Error != null)
          {
            System.Diagnostics.Trace.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", e.Error.Message));
          }
          client.Dispose();
        };

        client.Headers.Add("X-ApiKey", _apiKey);

        client.UploadStringAsync(RaygunSettings.Settings.ApiEndpoint, JObject.FromObject(raygunMessage, new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Ignore }).ToString());
      }
    }
#endif
  }
}
