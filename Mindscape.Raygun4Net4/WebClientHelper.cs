using System;
using System.Net;
using System.Reflection;
using Mindscape.Raygun4Net.Common.DataAccess;
using Mindscape.Raygun4Net.Logging;

namespace Mindscape.Raygun4Net
{


  public static class WebClientHelper
  {

    [ThreadStatic] private static WebClient _client;

    private static WebClient Client => _client ?? (_client = FreshClient());

    private static WebClient FreshClient()
    {
      return new RaygunWebClient();
    }

    private static readonly Uri ProxyUri;

    internal static IWebProxy WebProxy { get; set; }

    static WebClientHelper()
    {
      if (WebRequest.DefaultWebProxy != null)
      {
        var uri = new Uri(RaygunSettings.Settings.ApiEndpoint.ToString());
        ProxyUri = WebRequest.DefaultWebProxy.GetProxy(uri);
      }
    }

    internal static IHttpClient GetClient(string apiKey, ICredentials proxyCredentials)
    {

      Client.Headers.Clear();
      Client.Headers.Add("X-ApiKey", apiKey);
      Client.Headers.Add("content-type", "application/json; charset=utf-8");
      Client.Encoding = System.Text.Encoding.UTF8;

      if (WebProxy != null)
      {
        Client.Proxy = WebProxy;
      }
      else if (WebRequest.DefaultWebProxy != null)
      {
        if (ProxyUri != null && ProxyUri.AbsoluteUri != RaygunSettings.Settings.ApiEndpoint.ToString())
        {
          Client.Proxy = new WebProxy(ProxyUri, false);

          if (proxyCredentials == null)
          {
            Client.UseDefaultCredentials = true;
            Client.Proxy.Credentials = CredentialCache.DefaultCredentials;
          }
          else
          {
            Client.UseDefaultCredentials = false;
            Client.Proxy.Credentials = proxyCredentials;
          }
        }
      }

      return new WebClientFacade(Client);

    }


    public static void Send(string message, string apiKey, ICredentials proxyCredentials)
    {
      if (string.IsNullOrEmpty(apiKey))
      {
        RaygunLogger.Instance.Warning("ApiKey has not been provided, the Raygun message will not be sent");
        return;
      }

      var client = GetClient(apiKey, proxyCredentials);

      client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
    }
  }
}