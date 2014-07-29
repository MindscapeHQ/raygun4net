using System;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiClient : RaygunClientBase
  {
    private readonly ThreadLocal<HttpRequestMessage> _currentWebRequest = new ThreadLocal<HttpRequestMessage>(() => null);

    public RaygunWebApiClient(string apiKey) : base(apiKey) { }
    public RaygunWebApiClient() { }

    public static void Attach(HttpConfiguration config, Func<RaygunWebApiClient> generateRaygunClient = null)
    {
      var clientCreator = new RaygunWebApiClientProvider(generateRaygunClient);

      config.Services.Add(typeof(IExceptionLogger), new RaygunWebApiExceptionLogger(clientCreator));

      config.Filters.Add(new RaygunWebApiExceptionFilter(clientCreator));
      config.Filters.Add(new RaygunWebApiActionFilter(clientCreator));

      var concreteActivator = config.Services.GetHttpControllerActivator();
      config.Services.Replace(typeof(IHttpControllerActivator), new RaygunWebApiControllerActivator(concreteActivator, clientCreator));

      var concreteControllerSelector = config.Services.GetHttpControllerSelector() ?? new DefaultHttpControllerSelector(config);
      config.Services.Replace(typeof(IHttpControllerSelector), new RaygunWebApiControllerSelector(concreteControllerSelector, clientCreator));

      var concreteActionSelector = config.Services.GetActionSelector() ?? new ApiControllerActionSelector();
      config.Services.Replace(typeof(IHttpActionSelector), new RaygunWebApiActionSelector(concreteActionSelector, clientCreator));
    }

    public RaygunClientBase CurrentHttpRequest(HttpRequestMessage request)
    {
      _currentWebRequest.Value = request;
      return this;
    }

    protected override IRaygunMessageBuilder BuildMessageCore()
    {
      return RaygunWebApiMessageBuilder.New
        .SetHttpDetails(_currentWebRequest.Value, _requestMessageOptions);
    }
  }
}