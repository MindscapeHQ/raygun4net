using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunRequestMessage
  {
    public RaygunRequestMessage(HttpContext httpContext)
    {
      HostName = httpContext.Request.Url.Host;
      Url = httpContext.Request.Url.AbsolutePath;
      HttpMethod = httpContext.Request.RequestType;
      IPAddress = httpContext.Request.UserHostAddress;
      Data = ToDictionary(httpContext.Request.ServerVariables);
      QueryString = ToDictionary(httpContext.Request.QueryString);
      Headers = ToDictionary(httpContext.Request.Headers);
      Form = new NameValueCollection();

      foreach (string s in httpContext.Request.Form)
      {
        string name = s;
        string value = httpContext.Request.Form[s];

        if (s.Length <= 256 && value.Length <= 256)
        {
          Form.Add(s, httpContext.Request.Form[s]);
        }

        if (s.Length > 256)
        {
          name = s.Substring(0, 256);
        }
        if (value.Length > 256)
        {
          value = value.Substring(0, 256);
        }
        Form.Remove(s);
        Form.Add(name, value);
      }

      try
      {
        var contentType = httpContext.Request.Headers["Content-Type"];
        if (contentType != "text/html" && contentType != "application/x-www-form-urlencoded" && httpContext.Request.RequestType != "GET")
        {
          int length = 4096;
          string temp = new StreamReader(httpContext.Request.InputStream).ReadToEnd();
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

    private static IDictionary ToDictionary(NameValueCollection nameValueCollection)
    {
      var keys = nameValueCollection.AllKeys;
      var dictionary = new Dictionary<string, string>();

      foreach (string key in keys)
      {
        try
        {
          dictionary.Add(key, nameValueCollection[key]);
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

    public string HostName { get; set; }

    public string Url { get; set; }

    public string HttpMethod { get; set; }

    public string IPAddress { get; set; }

    public IDictionary QueryString { get; set; }

    public IDictionary Data { get; set; }

    public NameValueCollection Form { get; set; }

    public string RawData { get; set; }

    public IDictionary Headers { get; set; }

  }
}
