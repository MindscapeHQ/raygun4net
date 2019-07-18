using System;
using System.Runtime.Serialization;
using Mindscape.Raygun4Net.ProfilingSupport;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.Tests.ProfilingSupport
{
  [TestFixture]
  public class SettingsManagerTests
  {
    [Test]
    public void CanParseSettings()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",
      ""SamplingMethod"": 2,
      ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": [
        {
          ""Type"": 0,
          ""OverrideData"": ""{ \""SampleOption\"":\""Traces\"", \""Url\"":\""http://test-traces.com\"", \""SampleAmount\"":5, \""SampleBucketSize\"":10 }""
        },
        {
          ""Type"": 0,
          ""OverrideData"": ""{ \""SampleOption\"":\""Seconds\"", \""Url\"":\""http://test-seconds.com\"", \""SampleAmount\"":1, \""SampleBucketSize\"":5 }""
        }      
      ]
    }
  ]
}";

      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");

      CheckPolicyAndSampler(result.Policy,
        expectedSamplingMethod: DataSamplingMethod.Thumbprint,
        expectedConfig: "{ \"SampleAmount\":1, \"SampleIntervalAmount\":1, \"SampleIntervalOption\":2 }",
        expectedSamplerType: typeof(PerUriRateSampler));

      var perUriRateSampler = result.Policy.Sampler as PerUriRateSampler;
      Assert.NotNull(perUriRateSampler);
      Assert.AreEqual(1, perUriRateSampler.MaxPerInterval);
      Assert.AreEqual(0, perUriRateSampler.Interval.Seconds);
      Assert.AreEqual(1, perUriRateSampler.Interval.Minutes); // "SampleIntervalOptions": 2 => Minutes
      Assert.AreEqual(0, perUriRateSampler.Interval.Hours);

      Assert.AreEqual(2, result.Overrides.Count);

      var overrideTraces = result.Overrides[0];
      Assert.NotNull(overrideTraces);
      Assert.AreEqual(typeof(UrlSamplingOverride), overrideTraces.GetType()); // "Type": 0,
      Assert.AreEqual(new Uri("http://test-traces.com"), overrideTraces.Url);
      CheckPolicyAndSampler(overrideTraces.Policy,
        expectedSamplingMethod: DataSamplingMethod.Simple,
        expectedConfig: "{ \"SampleOption\":\"Traces\", \"Url\":\"http://test-traces.com\", \"SampleAmount\":5, \"SampleBucketSize\":10 }",
        expectedSamplerType: typeof(SimpleRateSampler)); // "SampleOption":"Traces" implies SimpleRateSampler

      var simpleRateSamplerOverride = overrideTraces.Policy.Sampler as SimpleRateSampler;
      Assert.NotNull(simpleRateSamplerOverride);
      Assert.AreEqual(5, simpleRateSamplerOverride.Take);
      Assert.AreEqual(10, simpleRateSamplerOverride.Limit);

      var overrideSeconds = result.Overrides[1];
      Assert.NotNull(overrideSeconds);
      Assert.AreEqual(typeof(UrlSamplingOverride), overrideSeconds.GetType()); // "Type": 0,
      Assert.AreEqual(new Uri("http://test-seconds.com"), overrideSeconds.Url);
      CheckPolicyAndSampler(overrideSeconds.Policy,
        expectedSamplingMethod: DataSamplingMethod.Thumbprint,
        expectedConfig: "{ \"SampleOption\":\"Seconds\", \"Url\":\"http://test-seconds.com\", \"SampleAmount\":1, \"SampleBucketSize\":5 }",
        expectedSamplerType: typeof(PerUriRateSampler)); // "SampleOption":"Seconds" (or Minutes/Hours) implies SimpleRateSampler

      var perUriRateSamplerOverride = overrideSeconds.Policy.Sampler as PerUriRateSampler;
      Assert.NotNull(perUriRateSamplerOverride);
      Assert.AreEqual(1, perUriRateSamplerOverride.MaxPerInterval);
      Assert.AreEqual(5, perUriRateSamplerOverride.Interval.Seconds);
      Assert.AreEqual(0, perUriRateSamplerOverride.Interval.Minutes);
      Assert.AreEqual(0, perUriRateSamplerOverride.Interval.Hours);
    }

    [Test]
    public void OnlyUrlOverridesAreProcessed()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",
      ""SamplingMethod"": 1,
      ""SamplingConfig"": ""{ \""SampleAmount\"": 1, \""SampleBucketSize\"": 2 }"",
      ""SamplingOverrides"": [
        {
          ""Type"": 1,
          ""OverrideData"": ""{\""SampleAmount\"":5, \""SampleBucketSize\"":10, \""SampleOption\"":\""Traces\"", \""Url\"":\""http://test-traces.com\""}""
        }    
      ]
    }
  ]
}";

      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");

      CheckPolicyAndSampler(result.Policy,
        expectedSamplingMethod: DataSamplingMethod.Simple,
        expectedConfig: "{ \"SampleAmount\": 1, \"SampleBucketSize\": 2 }",
        expectedSamplerType: typeof(SimpleRateSampler));

      var simpleRateSampler = result.Policy.Sampler as SimpleRateSampler;
      Assert.NotNull(simpleRateSampler);
      Assert.AreEqual(1, simpleRateSampler.Take);
      Assert.AreEqual(2, simpleRateSampler.Limit);

      // We only sample overides with "Type": 1 (UrlOverrides), all others are ignored
      Assert.AreEqual(0, result.Overrides.Count);
    }

    [Test]
    public void OnlyMatchingIdentifierIsParsed()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",
      ""SamplingMethod"": 2,
      ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": [
        {
          ""Type"": 0,
          ""OverrideData"": ""{ \""SampleOption\"":\""Traces\"", \""Url\"":\""http://test-traces.com\"", \""SampleAmount\"":5, \""SampleBucketSize\"":10 }""
        }
      ]
    }
  ]
}";

      var result = SettingsManager.ParseSamplingSettings(settingsJson, "NOT-TESTING");

      Assert.IsNull(result);
    }

    [Test]
    public void WithEmptyOverridesValidPolicyReturned()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",
      ""SamplingMethod"": 2,
      ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": []
    }
  ]
}";

      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");

      Assert.IsNotNull(result);
      Assert.IsNotNull(result.Policy);

      CheckPolicyAndSampler(result.Policy,
        expectedSamplingMethod: DataSamplingMethod.Thumbprint,
        expectedConfig: "{ \"SampleAmount\":1, \"SampleIntervalAmount\":1, \"SampleIntervalOption\":2 }",
        expectedSamplerType: typeof(PerUriRateSampler));

      Assert.IsNotNull(result.Overrides);
      Assert.IsEmpty(result.Overrides);
    }

    [Test]
    [ExpectedException(typeof(SerializationException))]
    public void InvalidJsonThrows()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING
      ""SamplingMethod"": 2,
      ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": []
    }
  ]
}";

      // "Identifier": "TESTING doesn't have a closing quote
      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void MissingIdentifierThrows()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {      
      ""SamplingMethod"": 2,
      ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": []
    }
  ]
}";

      // "Identifier": "TESTING" is MISSING
      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");
    }

    [Test]
    [ExpectedException(typeof(ArgumentException))]
    public void MissingIdentifierParameterThrows()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",
      ""SamplingMethod"": 2,
      ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": []
    }
  ]
}";

      var result = SettingsManager.ParseSamplingSettings(settingsJson, null /* this must be specified */);
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void MissingJsonPropertyForDefaultThrows()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",      
      ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": []
    }
  ]
}";

      // "SamplingMethod": 2, is MISSING
      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void MissingJsonPropertyForOverridesThrows()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",
      ""SamplingMethod"": 2,
      ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": [
        {
          ""Type"": 0,
          ""OverrideData"": ""{ \""SampleOption\"":\""Traces\"", \""SampleAmount\"":5, \""SampleBucketSize\"":10 }""
        }
      ]
    }
  ]
}";

      // \"Url\":\"http://test-traces.com\", is MISSING
      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void InvalidDefaultPolicyThrows()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",
      ""SamplingMethod"": 2,
      ""SamplingConfig"": ""{ \""SampleINVALIDAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": []
    }
  ]
}";

      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");
    }

    [Test]
    [ExpectedException(typeof(InvalidOperationException))]
    public void InvalidOverridesPolicyThrows()
    {
      var settingsJson = @"
{
  ""ApiKeyConfiguration"": [
    {
      ""Identifier"": ""TESTING"",
      ""SamplingMethod"": 2,
            ""SamplingConfig"": ""{ \""SampleAmount\"":1, \""SampleIntervalAmount\"":1, \""SampleIntervalOption\"":2 }"",
      ""SamplingOverrides"": [
        {
          ""Type"": 0,
          ""OverrideData"": ""{ \""SampleOption\"":\""Traces\"", \""Url\"":\""http://test-traces.com\"", \""SampleAmount\"":5, \""SampleINVALIDBucketSize\"":10 }""
        }
      ]
    }
  ]
}";

      var result = SettingsManager.ParseSamplingSettings(settingsJson, "TESTING");
    }

    private void CheckPolicyAndSampler(SamplingPolicy policy, DataSamplingMethod expectedSamplingMethod, string expectedConfig, Type expectedSamplerType)
    {
      Assert.NotNull(policy);
      Assert.NotNull(policy.Sampler);

      Assert.NotNull(expectedSamplingMethod);
      Assert.NotNull(expectedConfig);
      Assert.NotNull(expectedSamplerType);

      Assert.AreEqual(expectedSamplingMethod, policy.SamplingMethod);
      Assert.AreEqual(expectedConfig, policy.Configuration);
      Assert.AreEqual(expectedSamplerType, policy.Sampler.GetType());
    }
  }
}
