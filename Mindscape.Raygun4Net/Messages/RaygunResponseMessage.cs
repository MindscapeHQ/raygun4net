using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunResponseMessage
  {
    public int StatusCode { get; set; }

    public string StatusDescription { get; set; }
  }
}
