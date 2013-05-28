using System;
using System.Collections;
#if !WINRT && !WINDOWS_PHONE && !ANDROID
using System.Web;
#endif

using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunMessageBuilder
  {
    RaygunMessage Build();

    IRaygunMessageBuilder SetMachineName(string machineName);

    IRaygunMessageBuilder SetExceptionDetails(Exception exception);

    IRaygunMessageBuilder SetClientDetails();

    IRaygunMessageBuilder SetEnvironmentDetails();

    IRaygunMessageBuilder SetVersion();

    IRaygunMessageBuilder SetUserCustomData(IDictionary userCustomData);
  }
}