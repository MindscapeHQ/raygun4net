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

    private static IDictionary ToDictionary(NameValueCollection nameValueCollection)
    {
      var keys = nameValueCollection.AllKeys;

      return keys.ToDictionary(s => s, s => nameValueCollection[s]);
    }    

    public IDictionary QueryString { get; set; }    

    public IDictionary Data { get; set; }

    public NameValueCollection Form { get; set; }

    public string RawData { get; set; }

    public IDictionary Headers { get; set; }

  }
}