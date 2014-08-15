using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Mindscape.Raygun4Net.Messages;
using Mindscape.Raygun4Net.Messages.Builders;

namespace Mindscape.Raygun4Net
{
  public abstract class RaygunMessageBuilderBase : IRaygunMessageBuilder
  {
    protected readonly RaygunMessage _raygunMessage;

    protected RaygunMessageBuilderBase()
    {
      _raygunMessage = new RaygunMessage()
      {
        OccurredOn = DateTime.UtcNow,
        Details = new RaygunMessageDetails()
      };
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

    public virtual IRaygunMessageBuilder SetEnvironmentDetails()
    {
      try
      {
        _raygunMessage.Details.Environment = new RaygunEnvironmentMessageBuilder().Build();
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

    public virtual IRaygunMessageBuilder SetExceptionDetails(Exception exception)
    {
      if (exception != null)
      {
        _raygunMessage.Details.Error = new RaygunErrorMessageBuilder().Build(exception);
      }

      return this;
    }

    public IRaygunMessageBuilder SetClientDetails()
    {
      _raygunMessage.Details.Client = RaygunClientMessageBuilder.Build();
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