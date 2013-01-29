using System.Collections;
using System.Collections.Specialized;
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
      if (httpContext.Request.UrlReferrer != null) Referrer = httpContext.Request.UrlReferrer.ToString();
      UserAgent = httpContext.Request.UserAgent;
      Form = GetFormData(httpContext.Request.Form);
    }

    private NameValueCollection GetFormData(NameValueCollection nvc)
    {
      //int count;
   
    }

    private static IDictionary ToDictionary(NameValueCollection nameValueCollection)
    {
      var keys = nameValueCollection.AllKeys;

      return keys.ToDictionary(s => s, s => nameValueCollection[s]);
    }

    public string HostName { get; set; }

    public string Url { get; set; }

    public string HttpMethod { get; set; }

    public string IPAddress { get; set; }

    public IDictionary QueryString { get; set; }

    public IDictionary Headers { get; set; }

    public IDictionary Data { get; set; }

    public string UserAgent { get; set; }

    public string Referrer { get; set; }

    public NameValueCollection Form { get; set; }

    public string RawData { get; set; }

  }
}