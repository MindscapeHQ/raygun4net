using System.Collections;
using System.Collections.Generic;

namespace Mindscape.Raygun4Net.Messages
{
  public class RaygunMessageDetails
  {
    public string MachineName { get; set; }

    public string GroupingKey { get; set; }

    public string Version { get; set; }

    public string CorrelationId { get; set; }

    public string ContextId { get; set; }

    public RaygunErrorMessage Error { get; set; }

    public RaygunEnvironmentMessage Environment { get; set; }

    public RaygunClientMessage Client { get; set; }

    public IList<string> Tags { get; set; }

    public IDictionary UserCustomData { get; set; }

    public RaygunIdentifierMessage User { get; set; }

    public RaygunRequestMessage Request { get; set; }

    public RaygunResponseMessage Response { get; set; }

    public IList<RaygunBreadcrumb> Breadcrumbs { get; set; }
  }
}