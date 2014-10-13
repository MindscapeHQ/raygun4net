using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.WebApi.Builders;
using System.Diagnostics;
using Mindscape.Raygun4Net.Builders;
using System.Reflection;
using System.Collections;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiMessageBuilder : IRaygunMessageBuilder
  {
    public static RaygunWebApiMessageBuilder New
    {
      get { return new RaygunWebApiMessageBuilder(); }
    }

    private readonly RaygunMessage _raygunMessage;

    private RaygunWebApiMessageBuilder()
    {
      _raygunMessage = new RaygunMessage();
    }

    public RaygunMessage Build()
    {
      return _raygunMessage;
    }

    public IRaygunMessageBuilder SetMachineName(string machineName)
    {
      _raygunMessage.Details.MachineName = machineName;
      return this;
    }

    public IRaygunMessageBuilder SetEnvironmentDetails()
    {
      try
      {
        _raygunMessage.Details.Environment = RaygunEnvironmentMessageBuilder.Build();
      }
      catch (Exception ex)
      {
        // Different environments can fail to load the environment details.
        // For now if they fail to load for whatever reason then just
        // swallow the exception. A good addition would be to handle
        // these cases and load them correctly depending on where its running.
        // see http://raygun.io/forums/thread/3655
        Trace.WriteLine(string.Format("Failed to fetch the environment details: {0}", ex.Message));
      }

      return this;
    }

    public IRaygunMessageBuilder SetExceptionDetails(Exception exception)
    {
      if (exception != null)
      {
        _raygunMessage.Details.Error = RaygunErrorMessageBuilder.Build(exception);
      }

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

      return this;
    }

    public IRaygunMessageBuilder SetClientDetails()
    {
      _raygunMessage.Details.Client = new RaygunClientMessage();
      return this;
    }

    public IRaygunMessageBuilder SetUserCustomData(IDictionary userCustomData)
    {
      _raygunMessage.Details.UserCustomData = userCustomData;
      return this;
    }

    public IRaygunMessageBuilder SetTags(IList<string> tags)
    {
      _raygunMessage.Details.Tags = tags;
      return this;
    }

    public IRaygunMessageBuilder SetUser(RaygunIdentifierMessage user)
    {
      _raygunMessage.Details.User = user;
      return this;
    }

    public IRaygunMessageBuilder SetHttpDetails(HttpRequestDetails message)
    {
      if (message != null)
      {
        _raygunMessage.Details.Request = new RaygunWebApiRequestMessageBuilder().Build(message);
      }

      return this;
    }

    public IRaygunMessageBuilder SetHttpDetails(HttpRequestMessage message, RaygunRequestMessageOptions messageOptions = null)
    {
      return SetHttpDetails(new HttpRequestDetails(message, messageOptions));
    }

    public IRaygunMessageBuilder SetVersion(string version)
    {
      if (!String.IsNullOrEmpty(version))
      {
        _raygunMessage.Details.Version = version;
      }
      else
      {
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
          _raygunMessage.Details.Version = entryAssembly.GetName().Version.ToString();
        }
        else
        {
          _raygunMessage.Details.Version = "Not supplied";
        }
      }
      return this;
    }
  }
}