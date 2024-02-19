using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiActionSelector : IHttpActionSelector
  {
    private readonly IHttpActionSelector _concreteSelector;
    private readonly IRaygunWebApiClientProvider _clientCreator;

    internal RaygunWebApiActionSelector(IHttpActionSelector concreteSelector, IRaygunWebApiClientProvider clientCreator)
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
        _clientCreator.GenerateRaygunWebApiClient().SendInBackground(ex, new List<string> { RaygunWebApiClient.UnhandledExceptionTag });
        throw;
      }
    }

    public System.Linq.ILookup<string, HttpActionDescriptor> GetActionMapping(HttpControllerDescriptor controllerDescriptor)
    {
      return _concreteSelector.GetActionMapping(controllerDescriptor);
    }

    internal IHttpActionSelector ConcreteSelector
    {
      get { return _concreteSelector; }
    }
  }
}