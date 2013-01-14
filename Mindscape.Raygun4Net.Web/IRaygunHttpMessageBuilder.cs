using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Mindscape.Raygun4Net.Web
{
  public interface IRaygunHttpMessageBuilder
  {
    IRaygunMessageBuilder SetHttpDetails(HttpContext context);
  }
}
