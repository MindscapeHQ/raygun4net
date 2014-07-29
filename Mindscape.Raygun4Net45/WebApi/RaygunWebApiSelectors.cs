using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiActionSelector : IHttpActionSelector
  {
    private readonly IHttpActionSelector _concreteSelector;
    private readonly ICanCreateRaygunClient _clientCreator;

    internal RaygunWebApiActionSelector(IHttpActionSelector concreteSelector, ICanCreateRaygunClient clientCreator)
    {
      _concreteSelector = concreteSelector;
      _clientCreator = clientCreator;
    }

    public HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
    {
      try
      {
        return _concreteSelector.SelectAction(controllerContext);
      }
      catch (HttpResponseException ex)
      {
        _clientCreator.GetClient().CurrentHttpRequest(controllerContext.Request).Send(ex);
        throw;
      }
    }

    public System.Linq.ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
    {
      return _concreteSelector.GetActionMapping(controllerDescriptor);
    }
  }

  public class RaygunWebApiControllerSelector : IHttpControllerSelector
  {
    private readonly IHttpControllerSelector _concreteSelector;
    private readonly ICanCreateRaygunClient _clientCreator;

    internal RaygunWebApiControllerSelector(IHttpControllerSelector concreteSelector, ICanCreateRaygunClient clientCreator)
    {
      _concreteSelector = concreteSelector;
      _clientCreator = clientCreator;
    }

    public HttpControllerDescriptor SelectController(HttpRequestMessage request)
    {
      try
      {
        return _concreteSelector.SelectController(request);
      }
      catch (HttpResponseException ex)
      {
        _clientCreator.GetClient().CurrentHttpRequest(request).Send(ex);
        throw;
      }
    }

    public System.Collections.Generic.IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
    {
      return _concreteSelector.GetControllerMapping();
    }
  }
}