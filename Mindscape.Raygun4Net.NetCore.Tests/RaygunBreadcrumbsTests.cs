using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Breadcrumbs;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  [TestFixture]
  public class RaygunBreadcrumbsTests
  {
    [SetUp]
    public void SetUp()
    {
      RaygunBreadcrumbs.Storage = new AsyncLocalBreadcrumbStorage();
    }

    [Test]
    public void CanRecordBreadcrumb()
    {
      RaygunBreadcrumbs.Record("Test");

      Assert.That(RaygunBreadcrumbs.ToList().First().Message, Is.EqualTo("Test"));
    }

    [Test]
    public void ItSetsTheTimestamp()
    {
      RaygunBreadcrumbs.Record("Test");

      Assert.That(RaygunBreadcrumbs.ToList().First().Timestamp, Is.GreaterThan(0));
    }

    [Test]
    public void ItSetsClassNameAndMethodName()
    {
      var breadcrumbs = new BreadcrumbTestRecorder();
      breadcrumbs.Record();

      var crumb = RaygunBreadcrumbs.ToList().First();

      Assert.That(crumb.ClassName, Is.EqualTo("Mindscape.Raygun4Net.NetCore.Tests.RaygunBreadcrumbsTests"));
      Assert.That(crumb.MethodName, Is.EqualTo("ItSetsClassNameAndMethodName"));
    }

    [Test]
    public void CanClearStoredBreadcrumbs()
    {
      RaygunBreadcrumbs.Record("Test");
      
      Console.WriteLine(RaygunBreadcrumbs.ToList());

      Assert.That(RaygunBreadcrumbs.ToList(), Has.Count.EqualTo(1));

      RaygunBreadcrumbs.Clear();

      Assert.That(RaygunBreadcrumbs.ToList(), Is.Empty);
    }

    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task AsyncStorageOutOfScope()
    {
      // Make a new async context which is a level down from the current context
      await Task.Run(() => AsyncLowerContext());
      
      // Expect that we know nothing about the lower async context
      Assert.That(RaygunBreadcrumbs.ToList(), Is.Null);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Task AsyncLowerContext()
    {
      // We need to reassign the storage, because in the previous method the AsyncLocal<List<Breadcrumb>> was in scope
      // as it was set up by the SetUp method so any methods below get "flowed" into (correct behaviour of AsyncLocal)
      // and because the AsyncLocal does a shallow copy each level down it goes, that means that we'd be
      // accessing the same list object (which is wrong for our test, as we are trying to replicate when it is not
      // in context). Reassigning means that the context is only local to this level, and since it cannot flow back
      // up we get the expected behaviour
      
      RaygunBreadcrumbs.Storage = new AsyncLocalBreadcrumbStorage();
      RaygunBreadcrumbs.Record("Breadcrumb: out of context");
      
      return Task.CompletedTask;
    }

    [Test]
    public void InMemoryStorageInScope()
    {
      RaygunBreadcrumbs.Storage = new InMemoryBreadcrumbStorage();
      
      InMemoryLowerContext();
      
      Assert.That(RaygunBreadcrumbs.ToList(), Has.Count.EqualTo(1));
    }

    static void InMemoryLowerContext()
    {
      RaygunBreadcrumbs.Record("Breadcrumb: in context");
    }
      
  }
  
  
}