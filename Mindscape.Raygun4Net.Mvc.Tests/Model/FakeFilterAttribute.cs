using System.Web.Mvc;

namespace Mindscape.Raygun4Net.Mvc.Tests
{
  public class FakeFilterAttribute : FilterAttribute, IExceptionFilter
  {
    public void OnException(ExceptionContext filterContext)
    {
      // No-op
    }
  }
}
