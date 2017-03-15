using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;

namespace Mindscape.Raygun4Net.Azure.WebJob
{
  public static class JobHostConfigurationExtensions
  {
    public static void UseRaygun(this JobHostConfiguration config, string apiKey)
    {
      var processor = new RaygunExceptionHandler(apiKey);
      var traceMonitor = new TraceMonitor()
        .Filter(p => p.Exception != null, "Exception Handler")
        .Subscribe(processor.Process);

      config.Tracing.Tracers.Add(traceMonitor);
    }
  }
}
