using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunRequestMessage
  {
    public RaygunRequestMessage(HttpRequest	request, List<string> ignoredFormNames)
    {
      HostName = request.Url.Host;
      Url = request.Url.AbsolutePath;
      HttpMethod = request.RequestType;
      IPAddress = GetCorrectIpAddress(request);
      QueryString = ToDictionary(request.QueryString, Enumerable.Empty<string>());

      Headers = ToDictionary(request.Headers, ignoredFormNames ?? Enumerable.Empty<string>());
      Headers.Remove("Cookie");

      Form = ToDictionary(request.Form, ignoredFormNames ?? Enumerable.Empty<string>(), true);
      Cookies = GetCookies(request.Cookies, ignoredFormNames ?? Enumerable.Empty<string>());

      // Remove ignored and duplicated variables
      Data = ToDictionary(request.ServerVariables, ignoredFormNames ?? Enumerable.Empty<string>());
      Data.Remove("ALL_HTTP");
      Data.Remove("HTTP_COOKIE");
      Data.Remove("ALL_RAW");

      try
      {
        var contentType = request.Headers["Content-Type"];
        if (contentType != "text/html" && contentType != "application/x-www-form-urlencoded" && request.RequestType != "GET")
        {
          int length = 4096;
          string temp = new StreamReader(request.InputStream).ReadToEnd();
          if (length > temp.Length)
          {
            length = temp.Length;
          }

          RawData = temp.Substring(0, length);
        }
      }
      catch (HttpException)
      {
      }
    }

    public string GetCorrectIpAddress(HttpRequest request)
    {
        var strIp = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

        if (strIp != null && strIp.Trim().Length > 0)
        {
            if (strIp.Contains(","))
            {
                // first one = client IP per http://en.wikipedia.org/wiki/X-Forwarded-For
                strIp = strIp.Split(',')[0];
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

    public static bool IsValidIpAddress(string strIp)
    {
        if (strIp == null)
            return false;

        return Regex.IsMatch(strIp, "\\A(?:\\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\b)\\z");
    }

    private IList GetCookies(HttpCookieCollection cookieCollection, IEnumerable<string> ignoredFormNames)
    {
      var ignored = ignoredFormNames.ToLookup(s => s);

      return Enumerable.Range(0, cookieCollection.Count)
        .Select(i => cookieCollection[i])
        .Where(c => !ignored.Contains(c.Name))
        .Select(c => new Cookie(c.Name, c.Value))
        .ToList();
    }

    private static IDictionary ToDictionary(NameValueCollection nameValueCollection, IEnumerable<string> ignoreFields, bool truncateValues = false)
    {
      IEnumerable<string> keys;

      try
      {
        keys = nameValueCollection.AllKeys.Where(k => k != null).Except(ignoreFields);
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

    public class Cookie
    {
      public Cookie(string name, string value)
      {
        Name = name;
        Value = value;
      }

      public string Name { get; set; }
      public string Value { get; set; }
    }

    public string HostName { get; set; }

    public string Url { get; set; }

    public string HttpMethod { get; set; }

    public string IPAddress { get; set; }

    public IDictionary QueryString { get; set; }

    public IList Cookies { get; set; }

    public IDictionary Data { get; set; }

    public IDictionary Form { get; set; }

    public string RawData { get; set; }

    public IDictionary Headers { get; set; }

  }
}
