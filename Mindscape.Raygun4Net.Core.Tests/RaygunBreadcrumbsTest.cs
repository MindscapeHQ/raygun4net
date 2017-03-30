using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mindscape.Raygun4Net.Core.Tests.Models;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Core.Tests
{
  [TestFixture]
  public class RaygunBreadcrumbsTest
  {
    private RaygunBreadcrumbs _breadcrumbs;

    [SetUp]
    public void SetUp()
    {
      _breadcrumbs = new RaygunBreadcrumbs(new InMemoryBreadcrumbStorage());
    }

    [TearDown]
    public void TearDown()
    {
      RaygunSettings.Settings.BreadcrumbsLevel = RaygunBreadcrumbs.Level.Info;
    }

    [Test]
    public void You_Can_Record_A_Breadcrumb()
    {
      _breadcrumbs.Record("test");

      Assert.That(_breadcrumbs.First().Message, Is.EqualTo("test"));
    }

    [Test]
    public void It_Sets_The_Level_To_Info_If_Not_Set()
    {
      _breadcrumbs.Record("test");

      Assert.That(_breadcrumbs.First().Level, Is.EqualTo(RaygunBreadcrumbs.Level.Info));
    }

    [Test]
    public void It_Does_Not_Record_A_Breadcrumb_When_The_Breadcrumb_Level_Is_Too_High()
    {
      RaygunSettings.Settings.BreadcrumbsLevel = RaygunBreadcrumbs.Level.Error;

      _breadcrumbs.Record(new RaygunBreadcrumb() { Message = "test", Level = RaygunBreadcrumbs.Level.Info });

      Assert.That(_breadcrumbs, Is.Empty);
    }

    [Test]
    public void You_Can_Retrieve_Stored_Breadcrumbs()
    {
      _breadcrumbs = new RaygunBreadcrumbs(
         new InMemoryBreadcrumbStorage(
            new List<RaygunBreadcrumb>() { new RaygunBreadcrumb() }
         )
      );

      Assert.That(_breadcrumbs.Count(), Is.EqualTo(1));
    }

    [Test]
    public void You_Can_Clear_The_Stored_Breadcrumbs()
    {
      _breadcrumbs = new RaygunBreadcrumbs(
         new InMemoryBreadcrumbStorage(
            new List<RaygunBreadcrumb>() { new RaygunBreadcrumb() }
         )
      );

      _breadcrumbs.Clear();

      Assert.That(_breadcrumbs.Count(), Is.EqualTo(0));
    }
  }
}
