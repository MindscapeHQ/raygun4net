using System;
using Mindscape.Raygun4Net.Messages;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using MonoMac.Foundation;
using Mindscape.Raygun4Net.Builders;

namespace Mindscape.Raygun4Net
{
  public class RaygunMessageBuilder : IRaygunMessageBuilder
  {
    public static RaygunMessageBuilder New
    {
      get
      {
        return new RaygunMessageBuilder();
      }
    }

    private readonly RaygunMessage _raygunMessage;

    private RaygunMessageBuilder()
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
        System.Diagnostics.Debug.WriteLine(string.Format("Failed to fetch the environment details: {0}", ex.Message));
      }

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
      if (!String.IsNullOrWhiteSpace(version))
      {
        _raygunMessage.Details.Version = version;
      }
      else
      {
        try // So that the test can run.
        {
          if (NSBundle.MainBundle != null)
          {
            NSObject versionObject = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleShortVersionString");
            NSObject buildObject = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion");
            if (versionObject != null && buildObject != null)
            {
              _raygunMessage.Details.Version = versionObject + " (" + buildObject + ")";
            }
          }
        }
        catch (Exception e)
        {
          System.Diagnostics.Debug.WriteLine("Failed to get version: ", e.Message);
        }
      }

      if (_raygunMessage.Details.Version == null)
      {
        _raygunMessage.Details.Version = "Not supplied";
      }

      return this;
    }
  }
}
