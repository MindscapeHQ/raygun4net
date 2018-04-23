using System;
using System.Collections;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Builders
{
  public class RaygunNetCoreMessageBuilder : IRaygunMessageBuilder
  {
    private readonly RaygunMessage _raygunMessage;
    private readonly RaygunSettings _settings;
    
    public static RaygunNetCoreMessageBuilder New(RaygunSettings settings)
    {
      return new RaygunNetCoreMessageBuilder(settings);
    }

    private RaygunNetCoreMessageBuilder(RaygunSettings settings)
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
        #if NETSTANDARD2_0
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();

        if (entryAssembly != null)
        {
          _raygunMessage.Details.Version = entryAssembly.GetName().Version.ToString();
        }
        else
        {
          _raygunMessage.Details.Version = "Not supplied";
        }
        #else
        _raygunMessage.Details.Version = "Not supplied";
        #endif
      }
      
      return this;
    }
  }
}