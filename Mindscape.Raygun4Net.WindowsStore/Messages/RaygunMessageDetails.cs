using System.Collections;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessageDetails
  {
    public string MachineName { get; set; }

    public string Version { get; set; }

    public RaygunErrorMessage Error { get; set; }

    public RaygunEnvironmentMessage Environment { get; set; }

    public RaygunClientMessage Client { get; set; }

    public IList<string> Tags { get; set; }

    public IDictionary UserCustomData { get; set; }

    public RaygunIdentifierMessage User { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunMessageDetails: MachineName={0}, Version={1}, Error={2}, Environment={3}, Client={4}, Tags={5}, UserCustomData={6}, User={7}]", MachineName, Version, Error, Environment, Client, Tags, UserCustomData, User);
    }
  }
}