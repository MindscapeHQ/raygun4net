using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Mindscape.Raygun4Net.Builders
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
        // Don't send the raw request data at all if the content-type is urlencoded
        var contentType = request.ContentType;
        if (contentType != "text/html" &&
            (contentType == null ||
             CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "application/x-www-form-urlencoded",
               CompareOptions.IgnoreCase) < 0) && request.Method != "GET")
        {
          int length = 4096;
          request.Body.Seek(0, SeekOrigin.Begin);
          string temp = new StreamReader(request.Body).ReadToEnd();

          /*
           * In ASP.NET Core it seems the request.Form property doesn't get filled in unless it's an actual urlencoded form post.
           * because of this, the code that gets the ignored values and strips them out is not going to work at all.
           * To further complicate matters, since we can't rely on request.Form, we'd have to deserialize the request data which could be of virtually any type.
           */

          // If we made it this far, strip out any values that have been marked as ignored form fields
          //Dictionary<string, string> ignored = GetIgnoredFormValues(request.Form, options.IsFormFieldIgnored);
          //temp = StripIgnoredFormData(temp, ignored);

          if (length > temp.Length)
          {
            length = temp.Length;
          }

          return temp.Substring(0, length);
        }
        return null;
      }
      catch (Exception e)
      {
        return "Failed to retrieve raw data: " + e.Message;
      }
    }

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
      if (ip == null) return "";
      int? port = request.RemotePort == 0 ? request.LocalPort : request.RemotePort;

      if (port != 0) return ip + ":" + port.Value;

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
      foreach(var value in query.Where(v => isFormFieldIgnored(v.Key) == false))
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