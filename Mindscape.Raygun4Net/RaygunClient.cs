using System;
using System.Net;
using Mindscape.Raygun4Net.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if !WINRT
using System.Web;
#endif

namespace Mindscape.Raygun4Net
{
  public class RaygunClient
  {
    private readonly string _apiKey;

    public RaygunMessage Result { get; set; }

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
#endif
      }
      else
      {

#if !WINRT
        var message = RaygunMessageBuilder.New
                                      .SetHttpDetails(HttpContext.Current)
                                      .SetMachineName(Environment.MachineName)
                                      .SetExceptionDetails(exception)
                                      .SetClientDetails()
                                      .Build();
#else
        var message = RaygunMessageBuilder.New
              .SetHttpDetails(HttpContext.Current)
              .SetMachineName(Environment.MachineName)
              .SetExceptionDetails(exception)
              .SetClientDetails()
              .Build(); 
#endif

        Send(message);
      }
    }

    public void Send(RaygunMessage raygunMessage)
    {
#if !WINRT
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
      Result = raygunMessage;
#endif
    }
  }
}
