using System.Collections;
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
      if (httpContext.Request.UrlReferrer != null) Referrer = httpContext.Request.UrlReferrer.ToString();
      UserAgent = httpContext.Request.UserAgent;      
      Form = httpContext.Request.Form.ToString().Substring(0, 256);

      if (httpContext.Request.Headers["Content-Type"] != "text/html" && httpContext.Request.Headers["Content-Type"]
        != "application/x-www-form-urlencoded" && HttpMethod != "GET")
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

    public string Form { get; set; }

    public string RawData { get; set; }

  }
}