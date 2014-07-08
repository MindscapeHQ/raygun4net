using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.WebApi.Messages
{
  public class RaygunWebApiRequestMessage : IRaygunRequestMessage
  {
    public RaygunWebApiRequestMessage(HttpRequestMessage request, RaygunRequestMessageOptions options = null)
    {
      options = options ?? new RaygunRequestMessageOptions();

      HostName = request.RequestUri.Host;
      Url = request.RequestUri.AbsolutePath;
      HttpMethod = request.Method.ToString();
      IPAddress = GetIPAddress(request);

      Form = ToDictionary(request.GetQueryNameValuePairs(), options.IsFormFieldIgnored);

      SetHeadersAndRawData(request, options.IsHeaderIgnored);
    }

    private void SetHeadersAndRawData(HttpRequestMessage request, Func<string, bool> ignored)
    {
      Headers = new Dictionary<string, string>();

      foreach (var header in request.Headers.Where(h => !ignored(h.Key)))
      {
        Headers[header.Key] = string.Join(",", header.Value);
      }

      if (request.Content.Headers.ContentLength.HasValue && request.Content.Headers.ContentLength.Value > 0)
      {
        foreach (var header in request.Content.Headers)
        {
          Headers[header.Key] = string.Join(",", header.Value);
        }

        try
        {
          RawData = request.Content.ReadAsStringAsync().Result;
        }
        catch (Exception)
        {
        }
      }
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

    private const string HttpContext = "MS_HttpContext";
    private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";

    private string GetIPAddress(HttpRequestMessage request)
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
      return null;
    }

    public string HostName { get; set; }

    public string Url { get; set; }

    public string HttpMethod { get; set; }

    public string IPAddress { get; set; }

    public IDictionary Form { get; set; }

    public string RawData { get; set; }

    public IDictionary Headers { get; set; }

  }
}