using System;
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