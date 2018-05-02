using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tests.Model;

namespace Mindscape.Raygun4Net.Tests
{
  [TestFixture]
  public class RaygunBreadcrumbsTests
  {
    private RaygunBreadcrumbs GetBreadcrumbs(bool locationRecording = false, RaygunBreadcrumbLevel level = RaygunBreadcrumbLevel.Info)
    {
      var settings = new RaygunSettings() { BreadcrumbsLocationRecordingEnabled = locationRecording, BreadcrumbsLevel = level };
      return new RaygunBreadcrumbs(settings, new RaygunInMemoryBreadcrumbStorage());
    }

    [Test]
    public void Set_ClassName_MethodName_And_LineNumber_Automatically_If_Configured()
    {
      var breadcrumbs = GetBreadcrumbs(true);
      var test = new RaygunBreadcrumbTestRecorder(breadcrumbs);

      test.Record();
      var crumb = breadcrumbs.First();

      Assert.That(crumb.ClassName, Is.EqualTo("Tests.Model.RaygunBreadcrumbTestRecorder"));
      Assert.That(crumb.MethodName, Is.EqualTo("Record"));
    }

    [Test]
    public void You_Can_Record_A_Breadcrumb()
    {
      var breadcrumbs = GetBreadcrumbs();
      breadcrumbs.Store(new RaygunBreadcrumb() { Message = "test" });

      Assert.That(breadcrumbs.First().Message, Is.EqualTo("test"));
    }

    [Test]
    public void It_Sets_The_Timestamp()
    {
      var breadcrumbs = GetBreadcrumbs();
      breadcrumbs.Store(new RaygunBreadcrumb() { Message = "test" });

      Assert.That(breadcrumbs.First().Timestamp, Is.GreaterThan(0));
    }

    [Test]
    public void It_Sets_The_Level_To_Info_If_Not_Set()
    {
      var breadcrumbs = GetBreadcrumbs();
      breadcrumbs.Store(new RaygunBreadcrumb() { Message = "test" });

      Assert.That(breadcrumbs.First().Level, Is.EqualTo(RaygunBreadcrumbLevel.Info));
    }

    [Test]
    public void It_Does_Not_Record_A_Breadcrumb_When_The_Breadcrumb_Level_Is_Too_High()
    {
      var breadcrumbs = GetBreadcrumbs(false, RaygunBreadcrumbLevel.Error);
     
      breadcrumbs.Store(new RaygunBreadcrumb() { Message = "test", Level = RaygunBreadcrumbLevel.Info });

      Assert.That(breadcrumbs, Is.Empty);
    }

    [Test]
    public void You_Can_Retrieve_Stored_Breadcrumbs()
    {
      var breadcrumbs = new RaygunBreadcrumbs(new RaygunSettings(), new RaygunInMemoryBreadcrumbStorage(new List<RaygunBreadcrumb>() { new RaygunBreadcrumb() }));

      Assert.That(breadcrumbs.Count(), Is.EqualTo(1));
    }

    [Test]
    public void You_Can_Clear_The_Stored_Breadcrumbs()
    {
      var breadcrumbs = new RaygunBreadcrumbs(new RaygunSettings(), new RaygunInMemoryBreadcrumbStorage(new List<RaygunBreadcrumb>() { new RaygunBreadcrumb() }));

      breadcrumbs.Clear();

      Assert.That(breadcrumbs.Count(), Is.EqualTo(0));
    }
  }
}