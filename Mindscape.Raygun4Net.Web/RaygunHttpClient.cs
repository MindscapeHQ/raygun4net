using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Mindscape.Raygun4Net.Messages;

namespace Mindscape.Raygun4Net.Web
{
  public class RaygunHttpClient : RaygunClient
  {
    public RaygunHttpClient(string apiKey) : base(apiKey) { }

    public RaygunHttpClient() : this(RaygunSettings.Settings.ApiKey) { }

    public override RaygunMessage BuildMessage(Exception exception)
    {
      return RaygunMessageBuilder.New
        .SetHttpDetails(HttpContext.Current)
        .SetMachineName(Environment.MachineName)
        .SetExceptionDetails(exception)
        .SetClientDetails()
        .Build();        
    }
  }
}
