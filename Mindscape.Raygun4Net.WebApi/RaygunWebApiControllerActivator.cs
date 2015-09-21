using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiControllerActivator : IHttpControllerActivator
  {
    private readonly IHttpControllerActivator _concreteActivator;
    private readonly IRaygunWebApiClientProvider _clientCreator;

    internal RaygunWebApiControllerActivator(IHttpControllerActivator concreteActivator, IRaygunWebApiClientProvider clientCreator)
    {
      _concreteActivator = concreteActivator;
      _clientCreator = clientCreator;
    }

    public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
    {
      try
      {
        return _concreteActivator.Create(request, controllerDescriptor, controllerType);
      }
      catch(InvalidOperationException ex)
      {
        var client = _clientCreator.GenerateRaygunWebApiClient(new RaygunWebApiContext(request));
        client.SendInBackground(ex.InnerException);
        client.FlagExceptionAsSent(ex); // Stops us re-sending the outer exception
        throw;
      }
      catch(Exception ex)
      {
        _clientCreator.GenerateRaygunWebApiClient(new RaygunWebApiContext(request)).SendInBackground(ex);
        throw;
      }
    }

    internal IHttpControllerActivator ConcreteActivator
    {
      get { return _concreteActivator; }
    }
  }
}