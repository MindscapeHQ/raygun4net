using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunRequestMessageBuilder
  {
    private static readonly Regex IpAddressRegex = new Regex(@"\A(?:\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b)(:[1-9][0-9]{0,4})?\z", RegexOptions.Compiled);

    public static RaygunRequestMessage Build(HttpRequest request, RaygunRequestMessageOptions options)
    {
      RaygunRequestMessage message = new RaygunRequestMessage();

      options = options ?? new RaygunRequestMessageOptions();

      message.HostName = request.Url.Host;
      message.Url = request.Url.AbsolutePath;
      message.HttpMethod = request.RequestType;
      message.IPAddress = GetIpAddress(request);
      message.QueryString = ToDictionary(request.QueryString, null);

      message.Headers = ToDictionary(request.Headers, options.IsHeaderIgnored);
      message.Headers.Remove("Cookie");

      message.Form = ToDictionary(request.Form, options.IsFormFieldIgnored, true);
      message.Cookies = GetCookies(request.Cookies, options.IsCookieIgnored);

      // Remove ignored and duplicated variables
      message.Data = ToDictionary(request.ServerVariables, options.IsServerVariableIgnored);
      message.Data.Remove("ALL_HTTP");
      message.Data.Remove("HTTP_COOKIE");
      message.Data.Remove("ALL_RAW");

      if (!options.IsRawDataIgnored)
      {
        try
        {
          var contentType = request.Headers["Content-Type"];
          if (contentType != "text/html" && contentType != "application/x-www-form-urlencoded" && request.RequestType != "GET")
          {
            int length = 4096;
            request.InputStream.Seek(0, SeekOrigin.Begin);
            string temp = new StreamReader(request.InputStream).ReadToEnd();
            if (length > temp.Length)
            {
              length = temp.Length;
            }

            message.RawData = temp.Substring(0, length);
          }
        }
        catch (HttpException)
        {
        }
      }

      return message;
    }

    private static string GetIpAddress(HttpRequest request)
    {
      var strIp = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

      if (strIp != null && strIp.Trim().Length > 0)
      {
        string[] addresses = strIp.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (addresses.Length > 0)
        {
          // first one = client IP per http://en.wikipedia.org/wiki/X-Forwarded-For
          strIp = addresses[0];
        }
      }

      if (!IsValidIpAddress(strIp))
      {
        strIp = string.Empty;
      }

      // if that's empty, get their ip via server vars
      if (strIp == null || strIp.Trim().Length == 0)
      {
        strIp = request.ServerVariables["REMOTE_ADDR"];
      }

      if (!IsValidIpAddress(strIp))
      {
        strIp = string.Empty;
      }

      // if that's still empty, get their ip via .net's built-in method
      if (strIp == null || strIp.Trim().Length == 0)
      {
        strIp = request.UserHostAddress;
      }

      return strIp;
    }

    private static bool IsValidIpAddress(string strIp)
    {
      if (strIp != null)
      {
        return IpAddressRegex.IsMatch(strIp.Trim());
      }
      return false;
    }

    private delegate R Func<T, R>(T value);

    private static IList GetCookies(HttpCookieCollection cookieCollection, Func<string, bool> ignore)
    {
      IList cookies = new List<Mindscape.Raygun4Net.Messages.RaygunRequestMessage.Cookie>();

      foreach (string key in cookieCollection.Keys)
      {
        if (!ignore(key))
        {
          cookies.Add(new Mindscape.Raygun4Net.Messages.RaygunRequestMessage.Cookie(cookieCollection[key].Name, cookieCollection[key].Value));
        }
      }

      return cookies;
    }

    private static IDictionary ToDictionary(NameValueCollection nameValueCollection, Func<string, bool> ignore, bool truncateValues = false)
    {
      IEnumerable<string> keys;

      try
      {
        keys = Filter(nameValueCollection, ignore);
      }
      catch (HttpRequestValidationException)
      {
        return new Dictionary<string, string> { { "Values", "Not able to be retrieved" } };
      }

      var dictionary = new Dictionary<string, string>();

      foreach (string key in keys)
      {
        try
        {
          var keyToSend = key;
          var valueToSend = nameValueCollection[key];

          if (truncateValues)
          {
            if (keyToSend.Length > 256)
            {
              keyToSend = keyToSend.Substring(0, 256);
            }

            if (valueToSend != null && valueToSend.Length > 256)
            {
              valueToSend = valueToSend.Substring(0, 256);
            }
          }

          dictionary.Add(keyToSend, valueToSend);
        }
        catch (HttpRequestValidationException e)
        {
          // If changing QueryString to be of type string in future, will need to account for possible
          // illegal values - in this case it is contained at the end of e.Message along with an error message

          int firstInstance = e.Message.IndexOf('\"');
          int lastInstance = e.Message.LastIndexOf('\"');

          if (firstInstance != -1 && lastInstance != -1)
          {
            dictionary.Add(key, e.Message.Substring(firstInstance + 1, lastInstance - firstInstance - 1));
          }
          else
          {
            dictionary.Add(key, string.Empty);
          }
        }
      }

      return dictionary;
    }

    private static IEnumerable<string> Filter(NameValueCollection nameValueCollection, Func<string, bool> ignore)
    {
      foreach (string key in nameValueCollection)
      {
        if (key != null && (ignore == null || !ignore(key)))
        {
          yield return key;
        }
      }
    }
  }
}
