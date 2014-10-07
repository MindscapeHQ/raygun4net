using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mindscape.Raygun4Net.WebApi.Builders
{
  public class RaygunWebApiRequestMessageBuilder
  {
    public RaygunRequestMessage Build(HttpRequestDetails request)
    {
      RaygunRequestMessage message = new RaygunRequestMessage();

      message.HostName = request.RequestUri.Host;
      message.Url = request.RequestUri.AbsolutePath;
      message.HttpMethod = request.Method.ToString();
      message.IPAddress = request.IPAddress;

      message.Headers = request.Headers;
      message.RawData = request.RawData;

      return message;
    }
  }

  public class HttpRequestDetails
  {
    public Uri RequestUri { get; set; }
    public HttpMethod Method { get; set; }
    public IDictionary Form { get; set; }
    public IDictionary Headers { get; set; }
    public string IPAddress { get; set; }
    public string RawData { get; set; }

    public HttpRequestDetails(HttpRequestMessage message, RaygunRequestMessageOptions options = null)
    {
      options = options ?? new RaygunRequestMessageOptions();

      RequestUri = message.RequestUri;
      Method = message.Method;
      Form = ToDictionary(message.GetQueryNameValuePairs(), options.IsFormFieldIgnored);
      IPAddress = GetIPAddress(message);
      SetHeadersAndRawData(message, options.IsHeaderIgnored);
    }

    private IDictionary ToDictionary(IEnumerable<KeyValuePair<string, string>> kvPairs, Func<string, bool> ignored)
    {
      var dictionary = new Dictionary<string, string>();
      foreach (var pair in kvPairs.Where(kv => !ignored(kv.Key)))
      {
        dictionary[pair.Key] = pair.Value;
      }
      return dictionary;
    }

    private string GetIPAddress(HttpRequestMessage request)
    {
      try
      {
        if (request.Properties.ContainsKey(HttpContext))
        {
          dynamic ctx = request.Properties[HttpContext];
          if (ctx != null)
          {
            return ctx.Request.UserHostAddress;
          }
        }

        if (request.Properties.ContainsKey(RemoteEndpointMessage))
        {
          dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
          if (remoteEndpoint != null)
          {
            return remoteEndpoint.Address;
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine("Failed to get IP address: {0}", ex.Message);
      }
      return null;
    }

    private void SetHeadersAndRawData(HttpRequestMessage request, Func<string, bool> ignored)
    {
      Headers = new Dictionary<string, string>();

      foreach (var header in request.Headers.Where(h => !ignored(h.Key)))
      {
        Headers[header.Key] = string.Join(",", header.Value);
      }

      try
      {
        if (request.Content.Headers.ContentLength.HasValue && request.Content.Headers.ContentLength.Value > 0)
        {
          foreach (var header in request.Content.Headers)
          {
            Headers[header.Key] = string.Join(",", header.Value);
          }

          RawData = request.Content.ReadAsStringAsync().Result;
        }
      }
      catch (Exception) { }
    }

    private const string HttpContext = "MS_HttpContext";
    private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
  }
}
