#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Mindscape.Raygun4Net.Filters;

namespace Mindscape.Raygun4Net.AspNetCore.Builders
{
  public class RaygunAspNetCoreRequestMessageBuilder
  {
    private const int MAX_RAW_DATA_LENGTH = 4096; // bytes

    public static async Task<RaygunRequestMessage> Build(HttpContext? context, RaygunRequestMessageOptions? options)
    {
      if (context == null)
      {
        return new RaygunRequestMessage();
      }
      
      var request = context.Request;

      options = options ?? new RaygunRequestMessageOptions();

      var message = new RaygunRequestMessage
      {
        HostName    = request.Host.Value,
        Url         = request.GetDisplayUrl(),
        HttpMethod  = request.Method,
        IPAddress   = GetIpAddress(context.Connection),
        QueryString = GetQueryString(request, options),
        Cookies     = GetCookies(request, options),
        RawData     = GetRawData(request, options),
        Headers     = GetHeaders(request, options)
      };

      message.Form = await GetForm(request, options);

      return message;
    }

    private static string GetIpAddress(ConnectionInfo connection)
    {
      var ip = connection.RemoteIpAddress ?? connection.LocalIpAddress;

      if (ip == null)
      {
        return "";
      }

      int? port = connection.RemotePort == 0 ? connection.LocalPort : connection.RemotePort;

      if (port != 0)
      {
        return ip + ":" + port.Value;
      }

      return ip.ToString();
    }

    private static IDictionary GetQueryString(HttpRequest request, RaygunRequestMessageOptions options)
    {
      IDictionary queryString = null;
     
      try
      {
        queryString = ToDictionary(request.Query, options.IsQueryParameterIgnored, options.IsSensitiveFieldIgnored);
      }
      catch (Exception e)
      {
        queryString = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return queryString;
    }

    private static IList GetCookies(HttpRequest request, RaygunRequestMessageOptions options)
    {
      IList cookies;
      try
      {
	      cookies = request.Cookies.Where(c => !options.IsCookieIgnored(c.Key) && !options.IsSensitiveFieldIgnored(c.Key))
		      .Select(c => new RaygunRequestMessage.Cookie(c.Key, c.Value)).ToList();
      }
      // ReSharper disable once EmptyGeneralCatchClause
      catch (Exception e)
      {
	      cookies = new List<string>() { "Failed to retrieve cookies: " + e.Message };
      }

      return cookies;
    }

    private static string GetRawData(HttpRequest request, RaygunRequestMessageOptions options)
    {
      if (options.IsRawDataIgnored)
      {
        return null;
      }

      try
      {
        var contentType        = request.ContentType;
        var isTextHtml         = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "text/html", CompareOptions.IgnoreCase) >= 0;
        var isFormUrlEncoded   = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "application/x-www-form-urlencoded", CompareOptions.IgnoreCase) >= 0;
        var isHttpGet          = request.Method == "GET";
        var streamIsNull       = request.Body == Stream.Null;
        var streamIsRewindable = request.Body.CanSeek;

        if (streamIsNull || !streamIsRewindable || isHttpGet || isFormUrlEncoded || isTextHtml)
        {
          return null;
        }

        Dictionary<string, string> ignoredMultiPartFormData = null;

        if (contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "multipart/form-data", CompareOptions.IgnoreCase) >= 0)
        {
          // For multipart form data, gather up all the form names and values to be stripped out later.
          ignoredMultiPartFormData = GetIgnoredFormValues(request.Form, options.IsFormFieldIgnored);
        }

        request.Body.Seek(0, SeekOrigin.Begin);

        // If we are ignoring form fields, increase the max ammount that we read from the stream to make sure we include the entirety of any value that may be stripped later on.
        var length = MAX_RAW_DATA_LENGTH;

        if (ignoredMultiPartFormData != null && ignoredMultiPartFormData.Count > 0)
        {
          length += ignoredMultiPartFormData.Values.Max(s => s == null ? 0 : s.Length);
        }

        length = Math.Min(length, (int)request.Body.Length);

        // Read the stream

        var buffer = new byte[length];
        request.Body.Read(buffer, 0, length);

        string rawData = Encoding.UTF8.GetString(buffer);

        request.Body.Seek(0, SeekOrigin.Begin);

        // Strip out ignored form fields from multipart form data payloads.
        if (ignoredMultiPartFormData != null)
        {
          rawData = StripIgnoredFormData(rawData, ignoredMultiPartFormData);
        }

        // Filter out sensitive values.
        rawData = StripSensitiveValues(rawData, options);

        // Early escape if theres no data.
        if (string.IsNullOrEmpty(rawData))
        {
          return null;
        }

        // Ensure the raw data string is not too large (over 4096 bytes).
        if (rawData.Length <= MAX_RAW_DATA_LENGTH)
        {
          return rawData;
        }
        else
        {
          return rawData.Substring(0, MAX_RAW_DATA_LENGTH);
        }
      }
      catch (Exception e)
      {
        return "Failed to retrieve raw data: " + e.Message;
      }
    }

    // This is specific to multipart/form-data
    private static Dictionary<string, string> GetIgnoredFormValues(IFormCollection form, Func<string, bool> ignore)
    {
      Dictionary<string, string> ignoredFormValues = new Dictionary<string, string>();

      foreach (string key in form.Keys)
      {
        if (ignore(key))
        {
          ignoredFormValues.Add(key, form[key]);
        }
      }

      return ignoredFormValues;
    }

    // This is specific to multipart/form-data
    private static string StripIgnoredFormData(string rawData, Dictionary<string, string> ignored)
    {
      foreach (string key in ignored.Keys)
      {
        string toRemove = "name=\"" + key + "\"\r\n\r\n" + ignored[key];
        rawData = rawData.Replace(toRemove, "");
      }

      return rawData;
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

    private static IDictionary GetHeaders(HttpRequest request, RaygunRequestMessageOptions options)
    {
      IDictionary headers = new Dictionary<string, string>();
      try
      {
        foreach (var header in request.Headers.Where(h => !options.IsHeaderIgnored(h.Key) && !options.IsSensitiveFieldIgnored(h.Key)))
        {
          headers[header.Key] = string.Join(",", header.Value);
        }
      }
      catch (Exception e)
      {
        headers = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return headers;
    }

    private static async Task<IDictionary> GetForm(HttpRequest request, RaygunRequestMessageOptions options)
    {
      IDictionary form = null;

      try
      {
        if (request.HasFormContentType)
        {
          form = ToDictionary(await request.ReadFormAsync(), options.IsFormFieldIgnored, options.IsSensitiveFieldIgnored);
        }
      }
      catch (Exception e)
      {
        form = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return form;
    }

    private static IDictionary ToDictionary(IQueryCollection query, Func<string, bool> isQueryStringVariableIgnored, Func<string, bool> isSensitive)
    {
      var dict = new Dictionary<string, string>();

      foreach (var value in query.Where(v => isQueryStringVariableIgnored(v.Key) == false && isSensitive(v.Key) == false))
      {
        dict[value.Key] = string.Join(",", value.Value);
      }

      return dict;
    }

    private static IDictionary ToDictionary(IFormCollection query, Func<string, bool> isFormFieldIgnored, Func<string, bool> isSensitive)
    {
      var dict = new Dictionary<string, string>();

      foreach (var value in query.Where(v => isFormFieldIgnored(v.Key) == false && isSensitive(v.Key) == false))
      {
        dict[value.Key] = string.Join(",", value.Value);
      }

      return dict;
    }
  }
}