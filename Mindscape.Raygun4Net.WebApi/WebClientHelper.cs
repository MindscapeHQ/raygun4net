using System;
using System.Net;

namespace Mindscape.Raygun4Net.WebApi
{
    public static class WebClientHelper
    {
        private static WebClient _client;

        private static WebClient Client => _client ?? (_client = new WebClient());
        private static readonly Uri ProxyUri;


        static WebClientHelper()
        {
            ProxyUri = WebRequest.DefaultWebProxy.GetProxy(new Uri(RaygunSettings.Settings.ApiEndpoint.ToString()));
        }

        public static void Send(string message, string apiKey, ICredentials proxyCredentials)
        {
            Client.Headers.Clear();
            Client.Headers.Add("X-ApiKey", apiKey);
            Client.Headers.Add("content-type", "application/json; charset=utf-8");
            Client.Encoding = System.Text.Encoding.UTF8;

            if (WebRequest.DefaultWebProxy != null)
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

            Client.UploadString(RaygunSettings.Settings.ApiEndpoint, message);
        }
    }
}