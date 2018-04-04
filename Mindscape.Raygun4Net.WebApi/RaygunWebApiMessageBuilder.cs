using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.WebApi.Builders;
using Mindscape.Raygun4Net.Builders;
using System.Reflection;
using System.Collections;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiMessageBuilder : IRaygunMessageBuilder
  {
    public static RaygunWebApiMessageBuilder New
    {
      get
      {
        return new RaygunWebApiMessageBuilder();
      }
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
      _raygunMessage.Details.Environment = RaygunEnvironmentMessageBuilder.Build();
      return this;
    }

    public IRaygunMessageBuilder SetExceptionDetails(Exception exception)
    {
      if (exception != null)
      {
        _raygunMessage.Details.Error = RaygunErrorMessageBuilder.Build(exception);

        if (_raygunMessage.Details.Error != null)
        {
          AssignCorrelationId(_raygunMessage.Details);
        }
      }

      var error = exception as RaygunWebApiHttpException;
      if (error != null)
      {
        _raygunMessage.Details.Response = new RaygunResponseMessage
        {
          StatusCode = (int)error.StatusCode, 
          StatusDescription = error.ReasonPhrase,
          Content = error.Content,
        };
      }

      var responseException = exception as HttpResponseException;
      if (responseException != null)
      {
        string content = RaygunSettings.Settings.IsResponseContentIgnored ? null : responseException.Response.Content.ReadAsString();
        _raygunMessage.Details.Response = new RaygunResponseMessage
        {
          StatusCode = (int)responseException.Response.StatusCode,
          StatusDescription = responseException.Response.ReasonPhrase,
          Content = content
        };
      }

      return this;
    }

    private void AssignCorrelationId(RaygunMessageDetails details)
    {
      if (details != null && details.Error != null)
      {
        details.CorrelationId = GenerateCorrelationId(details.Error.ClassName);
      }
    }

    private string GenerateCorrelationId(string className)
    {
      return Guid.NewGuid().ToString();
    }

    public IRaygunMessageBuilder SetClientDetails()
    {
      _raygunMessage.Details.Client = new RaygunClientMessage() { Name = "Raygun4Net.WebApi" };
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

    public IRaygunMessageBuilder SetHttpDetails(RaygunRequestMessage message)
    {
      _raygunMessage.Details.Request = message;
      return this;
    }

    public IRaygunMessageBuilder SetHttpDetails(HttpRequestMessage request, RaygunRequestMessageOptions options)
    {
      return SetHttpDetails(RaygunWebApiRequestMessageBuilder.Build(request, options));
    }

    public IRaygunMessageBuilder SetVersion(string version)
    {
      if (!String.IsNullOrEmpty(version))
      {
        _raygunMessage.Details.Version = version;
      }	  
      else if (!String.IsNullOrEmpty(RaygunSettings.Settings.ApplicationVersion))
      {
        _raygunMessage.Details.Version = RaygunSettings.Settings.ApplicationVersion;
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

    public IRaygunMessageBuilder SetTimeStamp(DateTime? currentTime)
    {
      if (currentTime != null)
      {
        _raygunMessage.OccurredOn = currentTime.Value;
      }
      return this;
    }

    public IRaygunMessageBuilder SetContextId(string contextId)
    {
      _raygunMessage.Details.ContextId = contextId;

      return this;
    }
  }
}