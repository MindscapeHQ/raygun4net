using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.AspNet5.Builders
{
  public class RaygunOwinMessageBuilder : IRaygunMessageBuilder
  {

    public static RaygunOwinMessageBuilder New(RaygunSettings settings)
    {
      return new RaygunOwinMessageBuilder(settings);
    }

    private readonly RaygunMessage _raygunMessage;
    private RaygunSettings _settings;

    private RaygunOwinMessageBuilder(RaygunSettings settings)
    {
      _raygunMessage = new RaygunMessage();
      _settings = settings;
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
      }

      //var error = exception as UnhandledRequestException;
      //if (error != null)
      //{
      //  _raygunMessage.Details.Response = new RaygunResponseMessage
      //  {
      //    StatusCode = error.StatusCode,
      //    StatusDescription = error.ReasonPhrase
      //  };
      //}

      return this;
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

    public IRaygunMessageBuilder SetRequestDetails(RaygunRequestMessage message)
    {
      _raygunMessage.Details.Request = message;
      return this;
    }

    public IRaygunMessageBuilder SetVersion(string version)
    {
      if (!String.IsNullOrEmpty(version))
      {
        _raygunMessage.Details.Version = version;
      }
      else if (!String.IsNullOrEmpty(_settings.ApplicationVersion))
      {
        _raygunMessage.Details.Version = _settings.ApplicationVersion;
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