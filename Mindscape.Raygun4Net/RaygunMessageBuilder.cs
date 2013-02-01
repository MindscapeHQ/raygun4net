using System;
using System.Collections;
#if !WINRT
using System.Reflection;
using System.Web;
#else
using Windows.ApplicationModel;
#endif
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
      _raygunMessage.Details.Environment = new RaygunEnvironmentMessage();

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

#if !WINRT
    public IRaygunMessageBuilder SetHttpDetails(HttpContext context)    
    {
      if (context != null)
      {
        _raygunMessage.Details.Request = new RaygunRequestMessage(context);
      }

      return this;
    }

    public IRaygunMessageBuilder SetVersion()
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
      return this;
    }    

#else
    public IRaygunMessageBuilder SetVersion()
    {
      PackageVersion version = Package.Current.Id.Version;
      _raygunMessage.Details.Version = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Revision,
                                                     version.Build);
      return this;
    }
#endif    
  }
}