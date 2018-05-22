using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Text;

namespace Mindscape.Raygun4Net.AspNetCore.Builders
{
  public class RaygunAspNetCoreRequestMessageBuilder
  {
    public static async Task<RaygunRequestMessage> Build(HttpContext context, RaygunRequestMessageOptions options)
    {
      var request = context.Request;
      options = options ?? new RaygunRequestMessageOptions();

      var message = new RaygunRequestMessage
      {
        HostName = request.Host.Value,
        Url = request.GetDisplayUrl(),
        HttpMethod = request.Method,
        IPAddress = GetIpAddress(context.Connection),
        Form = await GetForm(options, request),
        Cookies = GetCookies(options, request),
        QueryString = GetQueryString(request),
        RawData = GetRawData(options, request),
        Headers = GetHeaders(request, options.IsHeaderIgnored)
      };

      return message;
    }

    private static async Task<IDictionary> GetForm(RaygunRequestMessageOptions options, HttpRequest request)
    {
      IDictionary dictionary = null;
      try
      {
        if (request.HasFormContentType)
        {
          dictionary = ToDictionary(await request.ReadFormAsync(), options.IsFormFieldIgnored);
        }
      }
      // ReSharper disable once EmptyGeneralCatchClause
      catch { }
      return dictionary;
    }

    private static IList GetCookies(RaygunRequestMessageOptions options, HttpRequest request)
    {
      IList cookies = null;
      try
      {
        if (request.HasFormContentType)
        {
          cookies = GetCookies(request.Cookies, options.IsCookieIgnored);
        }
      }
      // ReSharper disable once EmptyGeneralCatchClause
      catch { }
      return cookies;
    }

    private static IDictionary GetQueryString(HttpRequest request)
    {
      IDictionary queryString = null;
      try
      {
        queryString = ToDictionary(request.Query, f => false);
      }
      // ReSharper disable once EmptyGeneralCatchClause
      catch { }
      return queryString;
    }

    private static string GetRawData(RaygunRequestMessageOptions options, HttpRequest request)
    {
      if (options.IsRawDataIgnored)
      {
        return null;
      }

      try
      {
        var contentType = request.ContentType;

        var streamIsNull = request.Body == Stream.Null;
        var streamIsRewindable = request.Body.CanSeek;
        var isTextHtml = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "text/html", CompareOptions.IgnoreCase) >= 0;
        var isHttpGet = request.Method == "GET";
        var isFormUrlEncoded = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "application/x-www-form-urlencoded", CompareOptions.IgnoreCase) >= 0;

        if (streamIsNull || isTextHtml || isHttpGet || isFormUrlEncoded || !streamIsRewindable)
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
        var length = 4096;
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
          if (rawData.Length > 4096)
          {
            rawData = rawData.Substring(0, 4096);
          }
        }

        return rawData;
      }
      catch (Exception e)
      {
        return "Failed to retrieve raw data: " + e.Message;
      }
    }

    // This is specific to multipart/form-data
    protected static Dictionary<string, string> GetIgnoredFormValues(IFormCollection form, Func<string, bool> ignore)
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
    protected static string StripIgnoredFormData(string rawData, Dictionary<string, string> ignored)
    {
      foreach (string key in ignored.Keys)
      {
        string toRemove = "name=\"" + key + "\"\r\n\r\n" + ignored[key];
        rawData = rawData.Replace(toRemove, "");
      }
      return rawData;
    }

    private static List<RaygunRequestMessage.Cookie> GetCookies(IRequestCookieCollection cookies, Func<string, bool> isCookieIgnored)
    {
      return cookies.Where(c => !isCookieIgnored(c.Key)).Select(c => new RaygunRequestMessage.Cookie(c.Key, c.Value)).ToList();
    }

    private static string GetIpAddress(ConnectionInfo request)
    {
      var ip = request.RemoteIpAddress ?? request.LocalIpAddress;
      if (ip == null)
      {
        return "";
      }

      int? port = request.RemotePort == 0 ? request.LocalPort : request.RemotePort;

      if (port != 0)
      {
        return ip + ":" + port.Value;
      }

      return ip.ToString();
    }

    private static Dictionary<string, string> GetHeaders(HttpRequest request, Func<string, bool> ignored)
    {
      var headers = new Dictionary<string, string>();

      foreach (var header in request.Headers.Where(h => !ignored(h.Key)))
      {
        headers[header.Key] = string.Join(",", header.Value);
      }

      return headers;
    }

    private static IDictionary ToDictionary(IQueryCollection query, Func<string, bool> isFormFieldIgnored)
    {
      var dict = new Dictionary<string, string>();
      foreach (var value in query.Where(v => isFormFieldIgnored(v.Key) == false))
      {
        dict[value.Key] = string.Join(",", value.Value);
      }
      return dict;
    }

    private static IDictionary ToDictionary(IFormCollection query, Func<string, bool> isFormFieldIgnored)
    {
      var dict = new Dictionary<string, string>();
      foreach (var value in query.Where(v => isFormFieldIgnored(v.Key) == false))
      {
        dict[value.Key] = string.Join(",", value.Value);
      }
      return dict;
    }
  }
}