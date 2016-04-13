using System;
using System.Collections;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Builders;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.AspNet5.Builders
{
  public class RaygunAspNet5MessageBuilder : IRaygunMessageBuilder
  {
    public static RaygunAspNet5MessageBuilder New(RaygunSettings settings)
    {
      return new RaygunAspNet5MessageBuilder(settings);
    }

    private readonly RaygunMessage _raygunMessage;
    private readonly RaygunSettings _settings;

    private RaygunAspNet5MessageBuilder(RaygunSettings settings)
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
      _raygunMessage.Details.Environment = RaygunEnvironmentMessageBuilder.Build(_settings);
      return this;
    }

    public IRaygunMessageBuilder SetExceptionDetails(Exception exception)
    {
      if (exception != null)
      {
        _raygunMessage.Details.Error = RaygunErrorMessageBuilder.Build(exception);
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

    public RaygunAspNet5MessageBuilder SetRequestDetails(RaygunRequestMessage message)
    {
      _raygunMessage.Details.Request = message;
      return this;
    }

    public RaygunAspNet5MessageBuilder SetResponseDetails(RaygunResponseMessage message)
    {
      _raygunMessage.Details.Response = message;
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
        _raygunMessage.Details.Version = "Not supplied";

        // Requires something equivalent to GetEntryAssembly to exist in DNXCORE50.
        /*
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
          _raygunMessage.Details.Version = entryAssembly.GetName().Version.ToString();
        }
        else
        {
          _raygunMessage.Details.Version = "Not supplied";
        }
        */
      }
      return this;
    }
  }
}