using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mindscape.Raygun4Net
{
  public class RaygunClientBase
  {
    protected internal const string SentKey = "AlreadySentByRaygun";

    protected bool CanSend(Exception exception)
    {
      return exception == null || !exception.Data.Contains(SentKey) || false.Equals(exception.Data[SentKey]);
    }

    protected void FlagAsSent(Exception exception)
    {
      if (exception != null)
      {
        exception.Data[SentKey] = true;
      }
    }
  }
}
