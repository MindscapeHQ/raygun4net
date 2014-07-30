using System;
using System.Net;
using System.Reflection;
using System.Web;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public class RaygunMessageBuilder : RaygunMessageBuilderBase
  {
    public static RaygunMessageBuilder New
    {
      get
      {
        return new RaygunMessageBuilder();
      }
    }

    public override IRaygunMessageBuilder SetExceptionDetails(Exception exception)
    {
      HttpException error = exception as HttpException;
      if (error != null)
      {
        int code = error.GetHttpCode();
        string description = null;
        if (Enum.IsDefined(typeof(HttpStatusCode), code))
        {
          description = ((HttpStatusCode)code).ToString();
        }
        _raygunMessage.Details.Response = new RaygunResponseMessage() { StatusCode = code, StatusDescription = description };
      }

      WebException webError = exception as WebException;
      if (webError != null)
      {
        if (webError.Status == WebExceptionStatus.ProtocolError)
        {
          HttpWebResponse response = (HttpWebResponse)webError.Response;
          _raygunMessage.Details.Response = new RaygunResponseMessage() { StatusCode = (int)response.StatusCode, StatusDescription = response.StatusDescription };
        }
        else
        {
          _raygunMessage.Details.Response = new RaygunResponseMessage() { StatusDescription = webError.Status.ToString() };
        }
      }

      return base.SetExceptionDetails(exception);
    }

    public IRaygunMessageBuilder SetHttpDetails(HttpContext context, RaygunRequestMessageOptions options = null)
    {
      if (context != null)
      {
        HttpRequest request;
        try
        {
          request = context.Request;
        }
        catch (HttpException)
        {
          return this;
        }
        _raygunMessage.Details.Request = new RaygunRequestMessage(request, options ?? new RaygunRequestMessageOptions());
      }

      return this;
    }
  }
}