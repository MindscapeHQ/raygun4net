using System;
using System.Collections;
#if WINRT
using Windows.ApplicationModel;
#elif WINDOWS_PHONE

#elif ANDROID
using System.Reflection;
#elif IOS
using System.Reflection;
#else
using System.Diagnostics;
using System.Reflection;
using System.Web;
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
      try
      {
        _raygunMessage.Details.Environment = new RaygunEnvironmentMessage();
      }
      catch (Exception)
      {
        // Different environments can fail to load the environment details.
        // For now if they fail to load for whatever reason then just
        // swallow the exception. A good addition would be to handle
        // these cases and load them correctly depending on where its running.
        // see http://raygun.io/forums/thread/3655
        Trace.WriteLine("Failed to fetch the environment details");
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

#if WINRT
    public IRaygunMessageBuilder SetVersion()
    {
      PackageVersion version = Package.Current.Id.Version;
      _raygunMessage.Details.Version = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Revision,
                                                     version.Build);
      return this;
    }
#elif WINDOWS_PHONE
    public IRaygunMessageBuilder SetVersion()
    {
      //PackageVersion version = Package.Current.Id.Version;
      //_raygunMessage.Details.Version = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Revision,
      //                                               version.Build);
      return this;
    }
#elif ANDROID || IOS
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
#else
    public IRaygunMessageBuilder SetHttpDetails(HttpContext context)
    {
	    if(context != null)
	    {
		    HttpRequest request;
		    try
		    {
			    request = context.Request;
		    }
		    catch (HttpException)
		    {
			    return this;
		    }
		    _raygunMessage.Details.Request = new RaygunRequestMessage(request);
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
#endif
  }
}