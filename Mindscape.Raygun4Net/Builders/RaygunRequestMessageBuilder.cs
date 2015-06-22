using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
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

      try
      {
        message.HostName = request.Url.Host;
        message.Url = request.Url.AbsolutePath;
        message.HttpMethod = request.RequestType;
        message.IPAddress = GetIpAddress(request);
      }
      catch (Exception e)
      {
        System.Diagnostics.Trace.WriteLine("Failed to get basic request info: {0}", e.Message);
      }

      // Query string
      try
      {
        message.QueryString = ToDictionary(request.QueryString, null);
      }
      catch (Exception e)
      {
        if (message.QueryString == null)
        {
          message.QueryString = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
        }
      }

      // Headers
      try
      {
        message.Headers = ToDictionary(request.Headers, options.IsHeaderIgnored);
        message.Headers.Remove("Cookie");
      }
      catch (Exception e)
      {
        if (message.Headers == null)
        {
          message.Headers = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
        }
      }

      // Form fields
      try
      {
        message.Form = ToDictionary(request.Form, options.IsFormFieldIgnored, true);
      }
      catch (Exception e)
      {
        if (message.Form == null)
        {
          message.Form = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
        }
      }

      // Cookies
      try
      {
        message.Cookies = GetCookies(request.Cookies, options.IsCookieIgnored);
      }
      catch (Exception e)
      {
        System.Diagnostics.Trace.WriteLine("Failed to get cookies: {0}", e.Message);
      }

      // Server variables
      try
      {
        message.Data = ToDictionary(request.ServerVariables, options.IsServerVariableIgnored);
        message.Data.Remove("ALL_HTTP");
        message.Data.Remove("HTTP_COOKIE");
        message.Data.Remove("ALL_RAW");
      }
      catch (Exception e)
      {
        if (message.Data == null)
        {
          message.Data = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
        }
      }

      // Raw data
      if (!options.IsRawDataIgnored)
      {
        try
        {
          // Don't send the raw request data at all if the content-type is urlencoded
          var contentType = request.Headers["Content-Type"];
          if (contentType != "text/html" && (contentType == null || CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "application/x-www-form-urlencoded", CompareOptions.IgnoreCase) < 0) && request.RequestType != "GET")
          {
            int length = 4096;
            request.InputStream.Seek(0, SeekOrigin.Begin);
            string temp = new StreamReader(request.InputStream).ReadToEnd();

            // If we made it this far, strip out any values that have been marked as ignored form fields
            Dictionary<string, string> ignored = GetIgnoredFormValues(request.Form, options.IsFormFieldIgnored);
            temp = StripIgnoredFormData(temp, ignored);

            if (length > temp.Length)
            {
              length = temp.Length;
            }

            message.RawData = temp.Substring(0, length);
          }
        }
        catch (Exception e)
        {
          if (message.RawData == null)
          {
            message.RawData = "Failed to retrieve raw data: " + e.Message;
          }
        }
      }

      return message;
    }

    protected static Dictionary<string, string> GetIgnoredFormValues(NameValueCollection form, Func<string, bool> ignore)
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

    private static string GetIpAddress(HttpRequest request)
    {
      string strIp = null;

      try
      {
        strIp = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

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
      }
      catch (Exception ex)
      {
        System.Diagnostics.Trace.WriteLine("Failed to get IP address: {0}", ex.Message);
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

    private static IList GetCookies(HttpCookieCollection cookieCollection, Func<string, bool> ignore)
    {
      return Enumerable.Range(0, cookieCollection.Count)
        .Select(i => cookieCollection[i])
        .Where(c => !ignore(c.Name))
        .Select(c => new Mindscape.Raygun4Net.Messages.RaygunRequestMessage.Cookie(c.Name, c.Value))
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
      catch (Exception e)
      {
        return new Dictionary<string, string> { { "Failed to retrieve", e.Message } };
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
        catch (Exception e)
        {
          dictionary.Add(key, e.Message);
        }
      }

      return dictionary;
    }
  }
}
