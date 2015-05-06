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
      _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(context.Exception);
    }

#pragma warning disable 1998
    public override async Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
    {
      _clientCreator.GenerateRaygunWebApiClient(context.Request)
            .SendInBackground(context.Exception);
    }
#pragma warning restore 1998
  }
}