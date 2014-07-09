using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiExceptionLogger : ExceptionLogger
  {
    private readonly ICanCreateRaygunClient _clientCreator;

    internal RaygunWebApiExceptionLogger(ICanCreateRaygunClient generateRaygunClient)
    {
      _clientCreator = generateRaygunClient;
    }

    public override void Log(ExceptionLoggerContext context)
    {
      _clientCreator.GetClient().CurrentHttpRequest(context.Request).Send(context.Exception, new List<string> { "Exception Logger" });
    }

    public override Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
    {
      return Task.Factory.StartNew(() => _clientCreator.GetClient().CurrentHttpRequest(context.Request).Send(context.Exception, new List<string> { "Exception Logger" }), cancellationToken);
    }
  }
}

