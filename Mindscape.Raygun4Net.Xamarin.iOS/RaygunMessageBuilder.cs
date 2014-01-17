using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using System.Reflection;
using Mindscape.Raygun4Net.Messages;

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
        _raygunMessage.Details.Environment = new RaygunEnvironmentMessage();
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
        _raygunMessage.Details.Error = new RaygunErrorMessage(exception);
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

    public IRaygunMessageBuilder SetUser(string user)
    {
      if (user != null && user.Length > 0)
      {
        _raygunMessage.Details.User = new RaygunIdentifierMessage(user);
      }
      return this;
    }

    public IRaygunMessageBuilder SetVersion()
    {
      if (_raygunMessage.Details.Environment.PackageVersion != null)
      {
        _raygunMessage.Details.Version = _raygunMessage.Details.Environment.PackageVersion;
      }
      else
      {
        _raygunMessage.Details.Version = "Not supplied";
      }
      return this;
    }
  }
}