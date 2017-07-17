using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiExceptionLogger : ExceptionLogger
  {
    private readonly IRaygunWebApiClientProvider _clientCreator;

    private static readonly Task CompletedTask = Task.FromResult(false);

    internal RaygunWebApiExceptionLogger(IRaygunWebApiClientProvider generateRaygunClient)
    {
      _clientCreator = generateRaygunClient;
    }

    public override void Log(ExceptionLoggerContext context)
    {
      if (context.Exception is OperationCanceledException)
      {
        return;
      }
      _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(context.Exception);
    }

    public override Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
    {
      if (context.Exception is OperationCanceledException)
      {
        return CompletedTask;
      }
      _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(context.Exception);
      return CompletedTask;
    }
  }
}