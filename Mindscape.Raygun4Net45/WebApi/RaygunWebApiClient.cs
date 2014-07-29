using System;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.Filters;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiClient : RaygunClientBase
  {
    private readonly ThreadLocal<HttpRequestMessage> _currentWebRequest = new ThreadLocal<HttpRequestMessage>(() => null);

    private static RaygunWebApiExceptionFilter _exceptionFilter;
    private static RaygunWebApiActionFilter _actionFilter;

    public RaygunWebApiClient(string apiKey) : base(apiKey) { }
    public RaygunWebApiClient() { }

    /// <summary>
    /// Causes Raygun4Net to listen for exceptions.
    /// </summary>
    /// <param name="config">The HttpConfiguration to attach to.</param>
    /// <param name="generateRaygunClient">An optional function to provide a custom RaygunWebApiClient instance to use for reporting exceptions.</param>
    public static void Attach(HttpConfiguration config, Func<RaygunWebApiClient> generateRaygunClient = null)
    {
      Detach(config);

      var clientCreator = new RaygunWebApiClientProvider(generateRaygunClient);

      config.Services.Add(typeof(IExceptionLogger), new RaygunWebApiExceptionLogger(clientCreator));

      _exceptionFilter = new RaygunWebApiExceptionFilter(clientCreator);
      config.Filters.Add(_exceptionFilter);

      _actionFilter = new RaygunWebApiActionFilter(clientCreator);
      config.Filters.Add(_actionFilter);

      var concreteActivator = config.Services.GetHttpControllerActivator();
      config.Services.Replace(typeof(IHttpControllerActivator), new RaygunWebApiControllerActivator(concreteActivator, clientCreator));

      var concreteControllerSelector = config.Services.GetHttpControllerSelector() ?? new DefaultHttpControllerSelector(config);
      config.Services.Replace(typeof(IHttpControllerSelector), new RaygunWebApiControllerSelector(concreteControllerSelector, clientCreator));

      var concreteActionSelector = config.Services.GetActionSelector() ?? new ApiControllerActionSelector();
      config.Services.Replace(typeof(IHttpActionSelector), new RaygunWebApiActionSelector(concreteActionSelector, clientCreator));
    }

    /// <summary>
    /// Causes Raygun4Net to stop listening for exceptions.
    /// </summary>
    /// <param name="config">The HttpConfiguration to detach from.</param>
    public static void Detach(HttpConfiguration config)
    {
      if (_exceptionFilter != null)
      {
        int exceptionLoggerIndex = config.Services.FindIndex(typeof(IExceptionLogger), (o) => o is RaygunWebApiExceptionLogger);
        if (exceptionLoggerIndex != -1)
        {
          config.Services.RemoveAt(typeof(IExceptionLogger), exceptionLoggerIndex);
        }

        config.Filters.Remove(_exceptionFilter);
        config.Filters.Remove(_actionFilter);

        RaygunWebApiControllerActivator controllerActivator = config.Services.GetHttpControllerActivator() as RaygunWebApiControllerActivator;
        if (controllerActivator != null)
        {
          config.Services.Replace(typeof(IHttpControllerActivator), controllerActivator.ConcreteActivator);
        }

        RaygunWebApiControllerSelector controllerSelector = config.Services.GetHttpControllerSelector() as RaygunWebApiControllerSelector;
        if (controllerSelector != null)
        {
          config.Services.Replace(typeof(IHttpControllerSelector), controllerSelector.ConcreteSelector);
        }

        RaygunWebApiActionSelector actionSelector = config.Services.GetActionSelector() as RaygunWebApiActionSelector;
        if (actionSelector != null)
        {
          config.Services.Replace(typeof(IHttpActionSelector), actionSelector.ConcreteSelector);
        }

        _exceptionFilter = null;
        _actionFilter = null;
      }
    }

    internal RaygunClientBase CurrentHttpRequest(HttpRequestMessage request)
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