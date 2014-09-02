using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunRequestMessage
  {
    public RaygunRequestMessage(HttpRequest request, RaygunRequestMessageOptions options)
    {
      options = options ?? new RaygunRequestMessageOptions();

      HostName = request.Url.Host;
      Url = request.Url.AbsolutePath;
      HttpMethod = request.RequestType;
      IPAddress = request.UserHostAddress;
      
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

    private delegate R Func<T, R>(T value);

    private IList GetCookies(HttpCookieCollection cookieCollection, Func<string, bool> ignore)
    {
      IList cookies = new List<Cookie>();

      foreach (string key in cookieCollection.Keys)
      {
        if (!ignore(key))
        {
          cookies.Add(new Cookie(cookieCollection[key].Name, cookieCollection[key].Value));
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
