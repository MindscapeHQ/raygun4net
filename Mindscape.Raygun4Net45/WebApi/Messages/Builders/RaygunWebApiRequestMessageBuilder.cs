using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.WebApi.Messages.Builders
{
  public class RaygunWebApiRequestMessageBuilder
  {
    private RaygunRequestMessage _raygunWebApiRequestMessage = new RaygunRequestMessage();

    public RaygunRequestMessage Build(HttpRequestMessage request, RaygunRequestMessageOptions options = null)
    {
      options = options ?? new RaygunRequestMessageOptions();

      _raygunWebApiRequestMessage.HostName = request.RequestUri.Host;
      _raygunWebApiRequestMessage.Url = request.RequestUri.AbsolutePath;
      _raygunWebApiRequestMessage.HttpMethod = request.Method.ToString();
      _raygunWebApiRequestMessage.IPAddress = GetIPAddress(request);

      _raygunWebApiRequestMessage.Form = ToDictionary(request.GetQueryNameValuePairs(), options.IsFormFieldIgnored);

      SetHeadersAndRawData(request, options.IsHeaderIgnored);

      return _raygunWebApiRequestMessage;
    }

    private void SetHeadersAndRawData(HttpRequestMessage request, Func<string, bool> ignored)
    {
      _raygunWebApiRequestMessage.Headers = new Dictionary<string, string>();

      foreach (var header in request.Headers.Where(h => !ignored(h.Key)))
      {
        _raygunWebApiRequestMessage.Headers[header.Key] = string.Join(",", header.Value);
      }

      if (request.Content.Headers.ContentLength.HasValue && request.Content.Headers.ContentLength.Value > 0)
      {
        foreach (var header in request.Content.Headers)
        {
          _raygunWebApiRequestMessage.Headers[header.Key] = string.Join(",", header.Value);
        }

        try
        {
          _raygunWebApiRequestMessage.RawData = request.Content.ReadAsStringAsync().Result;
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
  }
}
