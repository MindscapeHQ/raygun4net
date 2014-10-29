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
      return exception == null || exception.Data == null || !exception.Data.Contains(SentKey) || false.Equals(exception.Data[SentKey]);
    }

    protected void FlagAsSent(Exception exception)
    {
      if (exception != null && exception.Data != null)
      {
        try
        {
          Type[] genericTypes = exception.Data.GetType().GetGenericArguments();
          if (genericTypes.Length > 0 && genericTypes[0].IsAssignableFrom(typeof(string)))
          {
            exception.Data[SentKey] = true;
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Trace.WriteLine(String.Format("Failed to flag exception as sent: {0}", ex.Message));
        }
      }
    }
  }
}
