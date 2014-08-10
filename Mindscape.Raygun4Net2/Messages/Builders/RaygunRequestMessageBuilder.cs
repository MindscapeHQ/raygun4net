using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace Mindscape.Raygun4Net.Messages.Builders
{
  public class RaygunRequestMessageBuilder
  {
    public RaygunRequestMessage Build(HttpRequest request, RaygunRequestMessageOptions options)
    {
      var raygunRequestMessage = new RaygunRequestMessage();
      options = options ?? new RaygunRequestMessageOptions();

      raygunRequestMessage.HostName = request.Url.Host;
      raygunRequestMessage.Url = request.Url.AbsolutePath;
      raygunRequestMessage.HttpMethod = request.RequestType;
      raygunRequestMessage.IPAddress = request.UserHostAddress;

      raygunRequestMessage.QueryString = ToDictionary(request.QueryString, null);

      raygunRequestMessage.Headers = ToDictionary(request.Headers, options.IsHeaderIgnored);
      raygunRequestMessage.Headers.Remove("Cookie");

      raygunRequestMessage.Form = ToDictionary(request.Form, options.IsFormFieldIgnored, true);
      raygunRequestMessage.Cookies = GetCookies(request.Cookies, options.IsCookieIgnored);

      // Remove ignored and duplicated variables
      raygunRequestMessage.Data = ToDictionary(request.ServerVariables, options.IsServerVariableIgnored);
      raygunRequestMessage.Data.Remove("ALL_HTTP");
      raygunRequestMessage.Data.Remove("HTTP_COOKIE");
      raygunRequestMessage.Data.Remove("ALL_RAW");

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

          raygunRequestMessage.RawData = temp.Substring(0, length);
        }
      }
      catch (HttpException)
      {
      }

      return raygunRequestMessage;
    }

    private delegate R Func<T, R>(T value);

    private IList GetCookies(HttpCookieCollection cookieCollection, Func<string, bool> ignore)
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
