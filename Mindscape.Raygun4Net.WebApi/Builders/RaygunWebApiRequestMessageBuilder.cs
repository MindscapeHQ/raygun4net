using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.WebApi.Builders
{
  public class RaygunWebApiRequestMessageBuilder
  {
    private const string HTTP_CONTEXT = "MS_HttpContext";
    private const string REMOTE_ENDPOINT_MESSAGE = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
    private const int MAX_KEY_LENGTH = 256; // Characters
    private const int MAX_VALUE_LENGTH = 256; // Characters

    public static RaygunRequestMessage Build(HttpRequestMessage request, RaygunRequestMessageOptions options)
    {
      options = options ?? new RaygunRequestMessageOptions();

      var message = new RaygunRequestMessage
      {
        IPAddress   = GetIPAddress(request),
        QueryString = GetQueryString(request, options),
        Form        = GetForm(request, options),
        RawData     = GetRawData(request, options),
        Headers     = GetHeaders(request, options)
      };

      try
      {
        message.HostName   = request.RequestUri.Host;
        message.Url        = request.RequestUri.AbsolutePath;
        message.HttpMethod = request.Method.ToString();
      }
      catch (Exception e)
      {
        System.Diagnostics.Trace.WriteLine("Failed to get basic request info: {0}", e.Message);
      }

      return message;
    }

    private static string GetIPAddress(HttpRequestMessage request)
    {
      try
      {
        if (request.Properties.ContainsKey(HTTP_CONTEXT))
        {
          dynamic ctx = request.Properties[HTTP_CONTEXT];

          if (ctx != null)
          {
            return ctx.Request.UserHostAddress;
          }
        }

        if (request.Properties.ContainsKey(REMOTE_ENDPOINT_MESSAGE))
        {
          dynamic remoteEndpoint = request.Properties[REMOTE_ENDPOINT_MESSAGE];

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

    private static IDictionary GetQueryString(HttpRequestMessage request, RaygunRequestMessageOptions options)
    {
      IDictionary queryString = null;

      try
      {
        queryString = ToDictionary(request.GetQueryNameValuePairs(), options.IsQueryParameterIgnored, options.IsSensitiveFieldIgnored);
      }
      catch (Exception e)
      {
        queryString = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return queryString;
    }

    private static IDictionary GetForm(HttpRequestMessage request, RaygunRequestMessageOptions options)
    {
      IDictionary form = new Dictionary<string, string>();

      try
      {
        if (request.Content.IsFormData())
        {
          return form;
        }

        var data = request.Content.ReadAsFormDataAsync().GetAwaiter().GetResult();
        form = ToDictionary(data, options.IsFormFieldIgnored, options.IsSensitiveFieldIgnored, true);
      }
      catch (Exception e)
      {
        form = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return form;
    }

    private static IDictionary GetHeaders(HttpRequestMessage request, RaygunRequestMessageOptions options)
    {
      IDictionary headers = new Dictionary<string, string>();

      try
      {
        foreach (var header in request.Headers.Where(h => !options.IsHeaderIgnored(h.Key) && !options.IsSensitiveFieldIgnored(h.Key)))
        {
          headers[header.Key] = string.Join(",", header.Value);
        }

        if (request.Content.Headers.ContentLength.HasValue && request.Content.Headers.ContentLength.Value > 0)
        {
          foreach (var header in request.Content.Headers)
          {
            if (!options.IsHeaderIgnored(header.Key) && !options.IsSensitiveFieldIgnored(header.Key))
            {
              headers[header.Key] = string.Join(",", header.Value);
            }

          }
        }
      }
      catch (Exception e)
      {
        headers = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return headers;
    }

    public static string GetRawData(HttpRequestMessage request, RaygunRequestMessageOptions options)
    {
      if (options.IsRawDataIgnored)
      {
        return null;
      }

      try
      {
        string rawData = null;

        object body;
        // Check to see if we stored the body?
        if (request.Properties.TryGetValue(RaygunWebApiDelegatingHandler.REQUEST_BODY_KEY, out body))
        {
          rawData = body.ToString();
        }

        // Filter out sensitive values.
        return StripSensitiveValues(rawData, options);
      }
      catch (Exception e)
      {
        return "Failed to retrieve raw data: " + e.Message;
      }
    }

    public static string StripSensitiveValues(string rawData, RaygunRequestMessageOptions options)
    {
      // Early escape if theres no data.
      if (string.IsNullOrEmpty(rawData))
      {
        return null;
      }

      // Find the parser we want to use.
      var filters = GetRawDataFilters(options);

      foreach (var filter in filters)
      {
        // Parse the raw data into a dictionary.
        if (filter.CanParse(rawData))
        {
          var filteredData = filter.Filter(rawData, options.SensitiveFieldNames());

          if (!string.IsNullOrEmpty(filteredData))
          {
            return filteredData;
          }
        }
      }

      // We have failed to parse and filter the raw data, so check if the data contains sensitive values and should be dropped.
      if (options.IsRawDataIgnoredWhenFilteringFailed && DataContains(rawData, options.SensitiveFieldNames()))
      {
        return null;
      }
      else
      {
        return rawData;
      }
    }

    private static IList<IRaygunDataFilter> GetRawDataFilters(RaygunRequestMessageOptions options)
    {
      var parsers = new List<IRaygunDataFilter>();

      if (options.GetRawDataFilters() != null && options.GetRawDataFilters().Count > 0)
      {
        parsers.AddRange(options.GetRawDataFilters());
      }

      if (options.UseXmlRawDataFilter)
      {
        parsers.Add(new RaygunXmlDataFilter());
      }

      if (options.UseKeyValuePairRawDataFilter)
      {
        parsers.Add(new RaygunKeyValuePairDataFilter());
      }

      return parsers;
    }

    public static bool DataContains(string data, List<string> values)
    {
      bool exists = false;

      foreach (var value in values)
      {
        if (data.Contains(value))
        {
          exists = true;
          break;
        }
      }

      return exists;
    }
    
    private static IDictionary ToDictionary(NameValueCollection collection, Func<string, bool> ignored, Func<string, bool> isSensitive, bool truncateValues = false)
    {
      var dictionary = new Dictionary<string, string>();

      foreach (string key in collection.AllKeys.Where(k => !ignored(k) && !isSensitive(k)))
      {
        var k = key;
        var value = collection[k];

        if (truncateValues)
        {
          if (k.Length > MAX_KEY_LENGTH)
          {
            k = k.Substring(0, MAX_KEY_LENGTH);
          }

          if (value is { Length: > MAX_VALUE_LENGTH })
          {
            value = value.Substring(0, MAX_VALUE_LENGTH);
          }
        }

        dictionary[k] = value;
      }

      return dictionary;
    }

    private static IDictionary ToDictionary(IEnumerable<KeyValuePair<string, string>> kvPairs, Func<string, bool> ignored, Func<string, bool> isSensitive, bool truncateValues = false)
    {
      var dictionary = new Dictionary<string, string>();

      foreach (var pair in kvPairs.Where(kv => !ignored(kv.Key) && !isSensitive(kv.Key)))
      {
        var key = pair.Key;
        var value = pair.Value;

        if (truncateValues)
        {
          if (key.Length > MAX_KEY_LENGTH)
          {
            key = key.Substring(0, MAX_KEY_LENGTH);
          }

          if (value != null && value.Length > MAX_VALUE_LENGTH)
          {
            value = value.Substring(0, MAX_VALUE_LENGTH);
          }
        }

        dictionary[key] = value;
      }

      return dictionary;
    }
  }
}
