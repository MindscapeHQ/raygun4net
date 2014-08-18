using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiExceptionLogger : ExceptionLogger
  {
    private readonly IRaygunWebApiClientProvider _clientCreator;

    internal RaygunWebApiExceptionLogger(IRaygunWebApiClientProvider generateRaygunClient)
    {
      _clientCreator = generateRaygunClient;
    }

    public override void Log(ExceptionLoggerContext context)
    {
      _clientCreator.GenerateRaygunWebApiClient().CurrentHttpRequest(context.Request).Send(context.Exception);
    }

    public override Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
    {
      return Task.Factory.StartNew(() => 
        _clientCreator.GenerateRaygunWebApiClient()
        .CurrentHttpRequest(context.Request)
        .Send(context.Exception),
      cancellationToken);
    }
  }
}

