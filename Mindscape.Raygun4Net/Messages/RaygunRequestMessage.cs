using System;
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
    private static readonly Regex IpAddressRegex = new Regex(@"\A(?:\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b)(:[1-9][0-9]{0,4})?\z", RegexOptions.Compiled);

    public RaygunRequestMessage(HttpRequest request, RaygunRequestMessageOptions options)
    {
      options = options ?? new RaygunRequestMessageOptions();

      HostName = request.Url.Host;
      Url = request.Url.AbsolutePath;
      HttpMethod = request.RequestType;
      IPAddress = GetIpAddress(request);
      QueryString = ToDictionary(request.QueryString, null);

      Headers = ToDictionary(request.Headers, options.IsHeaderIgnored);
      Headers.Remove("Cookie");

      Form = ToDictionary(request.Form, options.IsFormFieldIgnored, true);
      Cookies = GetCookies(request.Cookies, options.IsCookieIgnored);

      // Remove ignored and duplicated variables
      Data = ToDictionary(request.ServerVariables, options.IsServerVariableIgnored);
      Data.Remove("ALL_HTTP");
      Data.Remove("HTTP_COOKIE");
      Data.Remove("ALL_RAW");

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

          RawData = temp.Substring(0, length);
        }
      }
      catch (HttpException)
      {
      }
    }

    private string GetIpAddress(HttpRequest request)
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

    private static bool IsValidIpAddress(string strIp)
    {
      if (strIp != null)
      {
        return IpAddressRegex.IsMatch(strIp.Trim());
      }
      return false;
    }

    private IList GetCookies(HttpCookieCollection cookieCollection, Func<string, bool> ignore)
    {
      return Enumerable.Range(0, cookieCollection.Count)
        .Select(i => cookieCollection[i])
        .Where(c => !ignore(c.Name))
        .Select(c => new Cookie(c.Name, c.Value))
        .ToList();
    }

    private static IDictionary ToDictionary(NameValueCollection nameValueCollection, Func<string, bool> ignore, bool truncateValues = false)
    {
      IEnumerable<string> keys;

      try
      {
        if (ignore == null)
        {
          keys = nameValueCollection.AllKeys.Where(k => k != null);
        }
        else
        {
          keys = nameValueCollection.AllKeys.Where(k => !ignore(k));
        }
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
