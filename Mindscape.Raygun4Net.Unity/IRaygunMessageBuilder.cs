using System;
using System.Collections;
using System.Collections.Generic;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunMessageBuilder
  {
    RaygunMessage Build();

    IRaygunMessageBuilder SetMachineName(string machineName);

    IRaygunMessageBuilder SetExceptionDetails(string message, string stackTrace, string type);

    IRaygunMessageBuilder SetClientDetails();

    IRaygunMessageBuilder SetEnvironmentDetails();

    IRaygunMessageBuilder SetVersion(string version);

    IRaygunMessageBuilder SetUserCustomData(IDictionary userCustomData);

    IRaygunMessageBuilder SetTags(IList<string> tags);

    IRaygunMessageBuilder SetUser(string user);
  }
}