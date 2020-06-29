using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Filters;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiExceptionFilter : ExceptionFilterAttribute
  {
    private readonly IRaygunWebApiClientProvider _clientCreator;

    internal RaygunWebApiExceptionFilter(IRaygunWebApiClientProvider clientCreator)
    {
      _clientCreator = clientCreator;
    }

    public override void OnException(HttpActionExecutedContext context)
    {
      _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(context.Exception, new List<string> { RaygunWebApiClient.UnhandledExceptionTag });
    }

#pragma warning disable 1998
    public override async Task OnExceptionAsync(HttpActionExecutedContext context, CancellationToken cancellationToken)
    {
      _clientCreator.GenerateRaygunWebApiClient(context.Request).SendInBackground(context.Exception, new List<string> { RaygunWebApiClient.UnhandledExceptionTag });
    }
#pragma warning restore 1998
  }
}