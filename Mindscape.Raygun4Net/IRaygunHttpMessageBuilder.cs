using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net
{
  public interface IRaygunHttpMessageBuilder
  {
    IRaygunMessageBuilder SetHttpDetails(HttpContext context);
  }
}
