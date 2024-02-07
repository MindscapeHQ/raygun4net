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
using Mindscape.Raygun4Net.Filters;
using Mindscape.Raygun4Net.Logging;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunRequestMessageBuilder
  {
    private static readonly Regex IpAddressRegex = new Regex(@"\A(?:\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b)(:[1-9][0-9]{0,4})?\z", RegexOptions.Compiled);

    private const int MAX_RAW_DATA_LENGTH = 4096; // bytes
    private const int MAX_KEY_LENGTH = 256; // Characters
    private const int MAX_VALUE_LENGTH = 256; // Characters

    public static RaygunRequestMessage Build(HttpRequest request, RaygunRequestMessageOptions options)
    {
      options = options ?? new RaygunRequestMessageOptions();

      RaygunRequestMessage message = new RaygunRequestMessage()
      {
        IPAddress   = GetIpAddress(request),
        QueryString = GetQueryString(request, options),
        Cookies     = GetCookies(request, options),
        Data        = GetServerVariables(request, options),
        Form        = GetForm(request, options),
        RawData     = GetRawData(request, options),
        Headers     = GetHeaders(request, options)
      };

      try
      {
        message.HostName   = request.Url.Host;
        message.Url        = request.Url.AbsolutePath;
        message.HttpMethod = request.RequestType;
      }
      catch (Exception e)
      {
        RaygunLogger.Instance.Error($"Failed to get basic request info: { e.Message}");
      }

      return message;
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
        RaygunLogger.Instance.Error($"Failed to get IP address: {ex.Message}");
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

    private static IDictionary GetQueryString(HttpRequest request, RaygunRequestMessageOptions options)
    {
      IDictionary queryString = null;

      try
      {
        queryString = ToDictionary(request.QueryString, options.IsQueryParameterIgnored, options.IsSensitiveFieldIgnored);
      }
      catch (Exception e)
      {
        queryString = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return queryString;
    }

    private static IList GetCookies(HttpRequest request, RaygunRequestMessageOptions options)
    {
      return Enumerable.Range(0, request.Cookies.Count)
            .Select(i => request.Cookies[i])
            .Where(c => !options.IsCookieIgnored(c.Name) && !options.IsSensitiveFieldIgnored(c.Name))
            .Select(c => new Mindscape.Raygun4Net.Messages.RaygunRequestMessage.Cookie(c.Name, c.Value))
            .ToList();
    }

    private static IDictionary GetServerVariables(HttpRequest request, RaygunRequestMessageOptions options)
    {
      IDictionary serverVariables = new Dictionary<string, string>();
      try
      {
        serverVariables = ToDictionary(request.ServerVariables, options.IsServerVariableIgnored, options.IsSensitiveFieldIgnored);
        serverVariables.Remove("ALL_HTTP");
        serverVariables.Remove("HTTP_COOKIE");
        serverVariables.Remove("ALL_RAW");
      }
      catch (Exception e)
      {
        serverVariables = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return serverVariables;
    }

    private static IDictionary GetForm(HttpRequest request, RaygunRequestMessageOptions options)
    {
      IDictionary form = new Dictionary<string, string>();

      try
      {
        form = ToDictionary(request.Form, options.IsFormFieldIgnored, options.IsSensitiveFieldIgnored, true);
      }
      catch (Exception e)
      {
        form = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return form;
    }

    private static IDictionary GetHeaders(HttpRequest request, RaygunRequestMessageOptions options)
    {
      IDictionary headers = new Dictionary<string, string>();

      try
      {
        headers = ToDictionary(request.Headers, options.IsHeaderIgnored, options.IsSensitiveFieldIgnored);
        headers.Remove("Cookie");
      }
      catch (Exception e)
      {
        headers = new Dictionary<string, string>() { { "Failed to retrieve", e.Message } };
      }

      return headers;
    }

    public static string GetRawData(HttpRequest request, RaygunRequestMessageOptions options)
    {
      if (options.IsRawDataIgnored)
      {
        return null;
      }

      try
      {
        // Don't send the raw request data at all if the content-type is urlencoded
        var contentType        = request.Headers["Content-Type"];
        var isTextHtml         = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "text/html", CompareOptions.IgnoreCase) >= 0;
        var isFormUrlEncoded   = contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "application/x-www-form-urlencoded", CompareOptions.IgnoreCase) >= 0;
        var isHttpGet          = request.RequestType == "GET";
        var streamIsNull       = request.InputStream == Stream.Null;
        var streamIsRewindable = request.InputStream.CanSeek;

        if (streamIsNull || !streamIsRewindable || isHttpGet || isFormUrlEncoded || isTextHtml)
        {
          return null;
        }

        // Read the stream
        request.InputStream.Seek(0, SeekOrigin.Begin);
        string rawData = new StreamReader(request.InputStream).ReadToEnd();

        // Early escape if theres no data.
        if (string.IsNullOrEmpty(rawData))
        {
          return null;
        }

        Dictionary<string, string> ignoredMultiPartFormData = null;
        if (contentType != null && CultureInfo.InvariantCulture.CompareInfo.IndexOf(contentType, "multipart/form-data", CompareOptions.IgnoreCase) >= 0)
        {
          // For multipart form data, gather up all the form names and values to be stripped out later.
          ignoredMultiPartFormData = GetIgnoredFormValues(request.Form, options.IsFormFieldIgnored);
        }

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

    private static IDictionary ToDictionary(NameValueCollection nameValueCollection, Func<string, bool> ignore, Func<string, bool> isSensitive, bool truncateValues = false)
    {
      IEnumerable<string> keys;

      try
      {
        keys = nameValueCollection.AllKeys.Where(k => !ignore(k) && !isSensitive(k));
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
            if (keyToSend.Length > MAX_KEY_LENGTH)
            {
              keyToSend = keyToSend.Substring(0, MAX_KEY_LENGTH);
            }

            if (valueToSend != null && valueToSend.Length > MAX_VALUE_LENGTH)
            {
              valueToSend = valueToSend.Substring(0, MAX_VALUE_LENGTH);
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
