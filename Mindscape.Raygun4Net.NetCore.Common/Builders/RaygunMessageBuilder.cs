using System;
using System.Collections;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net
{
  public class RaygunMessageBuilder : IRaygunMessageBuilder
  {
    private readonly RaygunMessage _raygunMessage;
    private readonly RaygunSettingsBase _settings;
    
    public static RaygunMessageBuilder New(RaygunSettingsBase settings)
    {	
      return new RaygunMessageBuilder(settings);	
    }
    
    private RaygunMessageBuilder(RaygunSettingsBase settings)
    {
      _raygunMessage = new RaygunMessage();
      _settings = settings;
    }

    public RaygunMessage Build()
    {
      return _raygunMessage;
    }
    
    public IRaygunMessageBuilder SetTimeStamp(DateTime? currentTime)
    {
      if (currentTime != null)
      {
        _raygunMessage.OccurredOn = currentTime.Value;
      }

      return this;
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
      if (!string.IsNullOrEmpty(version))
      {
        _raygunMessage.Details.Version = version;
      }
      else if (!string.IsNullOrEmpty(_settings.ApplicationVersion))
      {
        _raygunMessage.Details.Version = _settings.ApplicationVersion;
      }
      else
      {  
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();

        _raygunMessage.Details.Version = entryAssembly?.GetName().Version?.ToString() ?? "Not supplied";
      }
      
      return this;
    }
    
    public IRaygunMessageBuilder SetRequestDetails(RaygunRequestMessage message)
    {
      _raygunMessage.Details.Request = message;
      return this;
    }

    public IRaygunMessageBuilder SetResponseDetails(RaygunResponseMessage message)
    {
      _raygunMessage.Details.Response = message;
      return this;
    }

    public IRaygunMessageBuilder Customise(Action<RaygunMessage> customiseMessage)
    {
      customiseMessage?.Invoke(_raygunMessage);
      return this;
    }
  }
}