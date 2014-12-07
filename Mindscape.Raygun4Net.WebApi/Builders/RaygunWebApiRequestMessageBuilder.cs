using Mindscape.Raygun4Net.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Mindscape.Raygun4Net.WebApi.Builders
{
  public class RaygunWebApiRequestMessageBuilder
  {
    private const string HttpContext = "MS_HttpContext";
    private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";

    public static RaygunRequestMessage Build(HttpRequestMessage request, RaygunRequestMessageOptions options)
    {
      var message = new RaygunRequestMessage();

      options = options ?? new RaygunRequestMessageOptions();

      message.HostName = request.RequestUri.Host;
      message.Url = request.RequestUri.AbsolutePath;
      message.HttpMethod = request.Method.ToString();
      message.IPAddress = GetIPAddress(request);
      message.Form = ToDictionary(request.GetQueryNameValuePairs(), options.IsFormFieldIgnored);
      message.QueryString = ToDictionary(request.GetQueryNameValuePairs(), s => false);

      if (!options.IsRawDataIgnored)
      {
        object body;
        if (request.Properties.TryGetValue(RaygunWebApiDelegatingHandler.RequestBodyKey, out body))
        {
          message.RawData = body.ToString();
        }
      }

      SetHeaders(message, request, options.IsHeaderIgnored);

      return message;
    }

    private static IDictionary ToDictionary(IEnumerable<KeyValuePair<string, string>> kvPairs, Func<string, bool> ignored)
    {
      var dictionary = new Dictionary<string, string>();
      foreach (var pair in kvPairs.Where(kv => !ignored(kv.Key)))
      {
        dictionary[pair.Key] = pair.Value;
      }
      return dictionary;
    }

    private static string GetIPAddress(HttpRequestMessage request)
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

    private static void SetHeaders(RaygunRequestMessage message, HttpRequestMessage request, Func<string, bool> ignored)
    {
      message.Headers = new Dictionary<string, string>();

      foreach (var header in request.Headers.Where(h => !ignored(h.Key)))
      {
        message.Headers[header.Key] = string.Join(",", header.Value);
      }

      try
      {
        if (request.Content.Headers.ContentLength.HasValue && request.Content.Headers.ContentLength.Value > 0)
        {
          foreach (var header in request.Content.Headers)
          {
            message.Headers[header.Key] = string.Join(",", header.Value);
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("Error retrieving Headers and RawData {0}", ex.Message);
      }
    }
  }
}
