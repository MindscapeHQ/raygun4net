using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
      RetrieveQueryString(httpContext.Request.QueryString);
    }

    private void RetrieveQueryString(NameValueCollection nameValueCollection)
    {
      QueryString = new Dictionary<string, string>();

      foreach (string key in nameValueCollection.Keys)
      {
        QueryString.Add(key, nameValueCollection[key]);
      }
    }

    public string HostName { get; set; }

    public string Url { get; set; }

    public string HttpMethod { get; set; }

    public string IPAddress { get; set; }

    public IDictionary QueryString { get; set; }
  }
}