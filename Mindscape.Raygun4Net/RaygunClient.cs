﻿using System;
using System.Net;
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
#else
using System.Web;
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

    public void Send(Exception exception)
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
#if !WINRT
        System.Diagnostics.Trace.WriteLine("ApiKey has not been provided, exception will not be logged");
#else
        System.Diagnostics.Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
#endif
      }
      else
      {

#if !WINRT
        var message = RaygunMessageBuilder.New
          .SetHttpDetails(HttpContext.Current)                                                                  
          .SetEnvironmentDetails()                                      
          .SetMachineName(Environment.MachineName)                                      
          .SetExceptionDetails(exception)
          .SetClientDetails()                                      
          .Build();
#else
        var message = RaygunMessageBuilder.New
          .SetEnvironmentDetails()
          .SetMachineName(NetworkInformation.GetHostNames()[0].DisplayName)
          .SetExceptionDetails(exception)
          .SetClientDetails()              
          .Build();
#endif

        Send(message);
      }
    }
#if !WINRT
    public void Send(RaygunMessage raygunMessage)
    {
      using (var client = new WebClient())
      {
        client.Headers.Add("X-ApiKey", _apiKey);

        try
        {
          client.UploadString(RaygunSettings.Settings.ApiEndpoint, JObject.FromObject(raygunMessage, new JsonSerializer { MissingMemberHandling = MissingMemberHandling.Ignore }).ToString());
        }
        catch (Exception ex)
        {
          System.Diagnostics.Trace.WriteLine(string.Format("Error Logging Exception to Raygun.io {0}", ex.Message));
        }
      }
    }
#else
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

    private async Task PostMessageAsync(HttpClient client, HttpContent httpContent, Uri uri)
    {
      HttpResponseMessage response;
      response = client.PostAsync(uri, httpContent).Result;
      client.Dispose();
    }

    public void Wrap(Action func)
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

    public TResult Wrap<TResult>(Func<TResult> func)
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
#endif
  }
}
