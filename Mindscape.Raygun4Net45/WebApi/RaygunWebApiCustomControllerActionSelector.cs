using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace Mindscape.Raygun4Net.WebApi
{
  public class RaygunWebApiCustomControllerActionSelector : ApiControllerActionSelector
  {
    private readonly ICanCreateRaygunClient _clientCreator;

    internal RaygunWebApiCustomControllerActionSelector(ICanCreateRaygunClient clientCreator)
    {
      _clientCreator = clientCreator;
    }

    public override HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
    {
      try
      {
        return base.SelectAction(controllerContext);
      }
      catch (HttpResponseException ex)
      {
        _clientCreator.GetClient().CurrentHttpRequest(controllerContext.Request).Send(ex);
        throw;
      }
    }
  }

  public class RaygunWebApiCustomControllerSelector : DefaultHttpControllerSelector
  {
    private readonly ICanCreateRaygunClient _clientCreator;

    internal RaygunWebApiCustomControllerSelector(HttpConfiguration configuration, ICanCreateRaygunClient clientCreator) : base(configuration)
    {
      _clientCreator = clientCreator;
    }

    public override HttpControllerDescriptor SelectController(HttpRequestMessage request)
    {
      try
      {
        return base.SelectController(request);
      }
      catch (HttpResponseException ex)
      {
        _clientCreator.GetClient().CurrentHttpRequest(request).Send(ex);
        throw;
      }
    }
  }
}