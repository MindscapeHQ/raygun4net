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
    public RaygunRequestMessage(HttpRequest	request, RaygunRequestMessageOptions options)
    {
      options = options ?? new RaygunRequestMessageOptions();

      HostName = request.Url.Host;
      Url = request.Url.AbsolutePath;
      HttpMethod = request.RequestType;
      IPAddress = request.UserHostAddress;
      QueryString = ToDictionary(request.QueryString, Enumerable.Empty<string>());

      Headers = ToDictionary(request.Headers, options.IgnoreHeaderNames);
      Headers.Remove("Cookie");

      Form = ToDictionary(request.Form, options.IgnoreFormFieldNames, true);
      Cookies = GetCookies(request.Cookies, options.IsCookieIgnored);

      // Remove ignored and duplicated variables
      Data = ToDictionary(request.ServerVariables, options.IgnoreServerVariableNames);
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

    private IList GetCookies(HttpCookieCollection cookieCollection, Func<string, bool> ignore)
    {
      /*if (ignoredCookies.Count() == 1 && ignoredCookies.Contains("*"))
      {
        return Enumerable.Empty<Cookie>().ToList();
      }

      HashSet<string> pureIgnores = new HashSet<string>();
      List<Regex> expressions = new List<Regex>();
      foreach (string ignore in ignoredCookies)
      {
        try
        {
          Regex regex = new Regex(ignore);
          expressions.Add(regex);
        }
        catch
        {
          pureIgnores.Add(ignore);
        }
      }*/

      return Enumerable.Range(0, cookieCollection.Count)
        .Select(i => cookieCollection[i])
        //.Where(c => !pureIgnores.Contains(c.Name))
        //.Where(c => !IgnoreCookie(c.Name, expressions))
        .Where(c => !ignore(c.Name))
        .Select(c => new Cookie(c.Name, c.Value))
        .ToList();
    }

    private bool IgnoreCookie(string name, List<Regex> expressions)
    {
      foreach (Regex regex in expressions)
      {
        Match match = regex.Match(name);
        if (match != null && match.Success)
        {
          return true;
        }
      }
      return false;
    }

    private static IDictionary ToDictionary(NameValueCollection nameValueCollection, IEnumerable<string> ignoreKeys, bool truncateValues = false)
    {
      var dictionary = new Dictionary<string, string>();

      if (ignoreKeys.Count() == 1 && "*".Equals(ignoreKeys.First()))
      {
        return dictionary;
      }

      IEnumerable<string> keys;

      try
      {
        keys = Filter(nameValueCollection, ignoreKeys);
      }
      catch (HttpRequestValidationException)
      {
        return new Dictionary<string, string> { { "Values", "Not able to be retrieved" } };
      }

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

    private static IEnumerable<string> Filter(NameValueCollection nameValueCollection, IEnumerable<string> ignoreFields)
    {
      List<string> pureIgnores = new List<string>();
      List<Regex> expressions = new List<Regex>();
      foreach (string ignore in ignoreFields)
      {
        try
        {
          Regex regex = new Regex(ignore);
          expressions.Add(regex);
        }
        catch
        {
          pureIgnores.Add(ignore);
        }
      }

      foreach (string key in nameValueCollection.AllKeys.Where(k => k != null).Except(pureIgnores))
      {
        bool send = true;
        foreach (Regex regex in expressions)
        {
          Match match = regex.Match(key);
          if (match != null && match.Success)
          {
            send = false;
            break;
          }
        }
        if (send)
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
