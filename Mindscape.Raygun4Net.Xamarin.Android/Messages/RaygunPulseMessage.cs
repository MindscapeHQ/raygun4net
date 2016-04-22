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
  public class RaygunPulseMessage
  {
    public RaygunPulseDataMessage[] EventData { get; set; }
  }
}