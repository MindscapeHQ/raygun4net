using System;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiExceptionHandlingControllerActivator : IHttpControllerActivator
  {
    private readonly IHttpControllerActivator _concreteActivator;
    private readonly ICanCreateRaygunClient _clientCreator;

    public RaygunWebApiExceptionHandlingControllerActivator(IHttpControllerActivator concreteActivator, ICanCreateRaygunClient clientCreator)
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
      catch(Exception ex)
      {
        _clientCreator.GetClient().CurrentHttpRequest(request).Send(ex);
        return null;
      }
    }


  }
}