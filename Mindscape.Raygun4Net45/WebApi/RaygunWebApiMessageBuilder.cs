using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.WebApi.Messages;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiMessageBuilder : RaygunMessageBuilderBase
  {
    public static RaygunWebApiMessageBuilder New
    {
      get { return new RaygunWebApiMessageBuilder(); }
    }

    public override IRaygunMessageBuilder SetExceptionDetails(Exception exception)
    {
      var error = exception as RaygunWebApiHttpException;
      if (error != null)
      {
        _raygunMessage.Details.Response = new RaygunResponseMessage
        {
          StatusCode = (int)error.StatusCode, 
          StatusDescription = error.StatusCode.ToString()
        };
      }

      var responseException = exception as HttpResponseException;
      if (responseException != null)
      {
        try
        {
          var task = responseException.Response.Content.ReadAsStringAsync();
          task.Wait();
          responseException.Data["Content"] = task.Result;
        }
        catch(Exception) {}

        _raygunMessage.Details.Response = new RaygunResponseMessage
        {
          StatusCode = (int)responseException.Response.StatusCode,
          StatusDescription = responseException.Response.ReasonPhrase
        };
      }

      return base.SetExceptionDetails(exception);
    }

    public IRaygunMessageBuilder SetHttpDetails(HttpRequestDetails message)
    {
      if (message != null)
      {
        _raygunMessage.Details.Request = new RaygunWebApiRequestMessage(message);
      }

      return this;
    }

    public IRaygunMessageBuilder SetHttpDetails(HttpRequestMessage message, RaygunRequestMessageOptions messageOptions = null)
    {
      return SetHttpDetails(new HttpRequestDetails(message, messageOptions));
    }
  }
}