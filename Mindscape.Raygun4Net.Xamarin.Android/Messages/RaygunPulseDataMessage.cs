using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunPulseDataMessage
  {
    public string SessionId { get; set; }

    public DateTime Timestamp { get; set; }

    public string Type { get; set; }

    public RaygunIdentifierMessage User { get; set; }

    public string Version { get; set; }

    public string OS { get; set; }

    public string OSVersion { get; set; }

    public string Platform { get; set; }

    public string Data { get; set; }
  }
}