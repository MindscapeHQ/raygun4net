using System;
using System.Web;

using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunMessageBuilder
  {
    RaygunMessage Build();

    IRaygunMessageBuilder SetMachineName(string machineName);

    IRaygunMessageBuilder SetExceptionDetails(Exception exception);

    IRaygunMessageBuilder SetHttpDetails(HttpContext context);

    IRaygunMessageBuilder SetClientDetails();
  }
}