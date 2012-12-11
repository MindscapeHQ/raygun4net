using System;

using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunErrorMessageBuilder
  {
    RaygunErrorMessage Build();

    IRaygunErrorMessageBuilder SetMessage(Exception exception);

    IRaygunErrorMessageBuilder SetStackTrace(Exception exception);

    IRaygunErrorMessageBuilder SetClassName(Exception exception);

    IRaygunErrorMessageBuilder SetData(Exception exception);
  }
}