using System;
using System.Collections.Generic;
using System.Threading;
using Mindscape.Raygun4Net.ProfilingSupport;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests.ProfilingSupport
{
  [TestFixture]
  public class SamplingManagerTests
  {
    [Test]
    public void CanTakeSamplesDefaultPolicyWithSimpleRateSampler()
    {
      var samplingManager = new SamplingManager();
      var config = "{ \"SampleAmount\":2, \"SampleBucketSize\":4 }";
      var defaultSamplingPolicy = new SamplingPolicy(DataSamplingMethod.Simple, config);
      samplingManager.SetSamplingPolicy(defaultSamplingPolicy);

      var sampler = defaultSamplingPolicy.Sampler as SimpleRateSampler;
      Assert.NotNull(sampler);
      Assert.AreEqual(2, sampler.Take);
      Assert.AreEqual(4, sampler.Limit);

      var uri = new Uri("http://test-url.com");
      Assert.IsTrue(samplingManager.TakeSample(uri));
      Assert.IsTrue(samplingManager.TakeSample(uri));
      Assert.IsFalse(samplingManager.TakeSample(uri));
      Assert.IsFalse(samplingManager.TakeSample(uri));
      Assert.IsTrue(samplingManager.TakeSample(uri));
    }

    [Test]
    public void CanTakeSamplesOverridesPolicyWithSimpleRateSampler()
    {
      var samplingManager = new SamplingManager();
      var defaultConfig = "{ \"SampleAmount\":2, \"SampleBucketSize\":4 }";
      var defaultSamplingPolicy = new SamplingPolicy(DataSamplingMethod.Simple, defaultConfig);
      var overrideConfig = "{ \"SampleAmount\":1, \"SampleBucketSize\":3 }";

      var overrideSamplingPolicy = new SamplingPolicy(DataSamplingMethod.Simple, overrideConfig);
      var urlSamplingOverride = new UrlSamplingOverride("http://test-override.com", overrideSamplingPolicy);      
      samplingManager.SetSamplingPolicy(defaultSamplingPolicy, new List<UrlSamplingOverride>(new[] { urlSamplingOverride }));

      var defaultSampler = defaultSamplingPolicy.Sampler as SimpleRateSampler;
      Assert.NotNull(defaultSampler);
      Assert.AreEqual(2, defaultSampler.Take);
      Assert.AreEqual(4, defaultSampler.Limit);
      
      var overriderSampler = overrideSamplingPolicy.Sampler as SimpleRateSampler;
      Assert.NotNull(overriderSampler);
      Assert.AreEqual(1, overriderSampler.Take);
      Assert.AreEqual(3, overriderSampler.Limit);

      var defaultUri = new Uri("http://test-url.com");
      Assert.IsTrue(samplingManager.TakeSample(defaultUri));
      Assert.IsTrue(samplingManager.TakeSample(defaultUri));
      Assert.IsFalse(samplingManager.TakeSample(defaultUri));
      Assert.IsFalse(samplingManager.TakeSample(defaultUri));
      Assert.IsTrue(samplingManager.TakeSample(defaultUri));

      var overrideUri = new Uri("http://test-override.com");
      Assert.IsTrue(samplingManager.TakeSample(overrideUri));
      Assert.IsFalse(samplingManager.TakeSample(overrideUri));
      Assert.IsFalse(samplingManager.TakeSample(overrideUri));
      Assert.IsTrue(samplingManager.TakeSample(overrideUri));
      Assert.IsFalse(samplingManager.TakeSample(overrideUri));
      Assert.IsFalse(samplingManager.TakeSample(overrideUri));
    }

    [Test]
    public void CanTakeSamplesDefaultPolicyWithPerUriRateSampler()
    {
      var samplingManager = new SamplingManager();
      var config = "{ \"SampleAmount\":1, \"SampleIntervalAmount\":2, \"SampleIntervalOption\":1 }";
      var defaultSamplingPolicy = new SamplingPolicy(DataSamplingMethod.Thumbprint, config);
      samplingManager.SetSamplingPolicy(defaultSamplingPolicy);

      var perUriRateSampler = defaultSamplingPolicy.Sampler as PerUriRateSampler;
      Assert.NotNull(perUriRateSampler);
      Assert.AreEqual(1, perUriRateSampler.MaxPerInterval);
      Assert.AreEqual(2, perUriRateSampler.Interval.Seconds); // "SampleIntervalOptions": 1 => Seconds
      Assert.AreEqual(0, perUriRateSampler.Interval.Minutes); 
      Assert.AreEqual(0, perUriRateSampler.Interval.Hours);

      var uri = new Uri("http://test-url.com");
      Assert.IsTrue(samplingManager.TakeSample(uri));
      Assert.IsFalse(samplingManager.TakeSample(uri));
      Assert.IsFalse(samplingManager.TakeSample(uri));
      Thread.Sleep(2500); // at least 2 seconds
      Assert.IsTrue(samplingManager.TakeSample(uri));
      Assert.IsFalse(samplingManager.TakeSample(uri));
      Assert.IsFalse(samplingManager.TakeSample(uri));
    }
  }
}
