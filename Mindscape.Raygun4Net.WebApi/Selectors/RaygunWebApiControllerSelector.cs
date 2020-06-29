using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiControllerSelector : IHttpControllerSelector
  {
    private readonly IHttpControllerSelector _concreteSelector;
    private readonly IRaygunWebApiClientProvider _clientCreator;

    internal RaygunWebApiControllerSelector(IHttpControllerSelector concreteSelector, IRaygunWebApiClientProvider clientCreator)
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
        _clientCreator.GenerateRaygunWebApiClient(request).SendInBackground(ex, new List<string> { RaygunWebApiClient.UnhandledExceptionTag });
        throw;
      }
    }

    public IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
    {
      return _concreteSelector.GetControllerMapping();
    }

    internal IHttpControllerSelector ConcreteSelector
    {
      get { return _concreteSelector; }
    }
  }
}