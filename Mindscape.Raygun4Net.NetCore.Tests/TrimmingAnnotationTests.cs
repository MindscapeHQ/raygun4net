#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Mindscape.Raygun4Net.Offline;
using Mindscape.Raygun4Net.Storage;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
  /// <summary>
  /// SimpleJson reads payload properties with reflection, so trimmed / Native AOT apps only keep them because of the
  /// [DynamicDependency] attributes on the serialization methods (GitHub #570). These tests fail when a new type is
  /// added to the payload without a matching attribute. Run Mindscape.Raygun4Net.NetCore.TrimmingTests to verify a
  /// real trimmed publish.
  /// </summary>
  [TestFixture]
  public class TrimmingAnnotationTests
  {
    [Test]
    public void SerializeMessage_PreservesPropertiesOfEveryTypeInTheRaygunMessageGraph()
    {
      var preserved = PreservedTypes(typeof(RaygunClientBase), nameof(RaygunClientBase.SerializeMessage), DynamicallyAccessedMemberTypes.PublicProperties);

      var reachable = ReachablePayloadTypes(typeof(RaygunMessage));
      // Not discoverable from property types: AspNetCore stores these in the non-generic RaygunRequestMessage.Cookies list.
      reachable.Add(typeof(RaygunRequestMessage.Cookie));

      reachable.Except(preserved).Should().BeEmpty(
        "every type serialized as part of a RaygunMessage needs a [DynamicDependency] on RaygunClientBase.SerializeMessage, otherwise trimmed apps send it as {}");
    }

    [Test]
    public void OfflineStoreGetAll_PreservesConstructorAndPropertiesOfCrashReportStoreEntry()
    {
      var preserved = PreservedTypes(typeof(FileSystemCrashReportStore), nameof(FileSystemCrashReportStore.GetAll),
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties);

      preserved.Should().Contain(typeof(CrashReportStoreEntry));
    }

    private static HashSet<Type> PreservedTypes(Type declaringType, string methodName, DynamicallyAccessedMemberTypes required)
    {
      var method = declaringType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
      method.Should().NotBeNull();

      return method!.GetCustomAttributes<DynamicDependencyAttribute>()
                    .Where(a => a.Type != null && (a.MemberTypes & required) == required)
                    .Select(a => a.Type!)
                    .ToHashSet();
    }

    /// <summary>
    /// Walks public properties from <paramref name="root"/>, unwrapping arrays and generic arguments, and returns
    /// every class from the Raygun assembly that SimpleJson would serialize by reflecting over its properties.
    /// </summary>
    private static HashSet<Type> ReachablePayloadTypes(Type root)
    {
      var raygunAssembly = typeof(RaygunMessage).Assembly;
      var found = new HashSet<Type>();
      var pending = new Stack<Type>();
      pending.Push(root);

      while (pending.Count > 0)
      {
        var type = pending.Pop();

        if (type.IsArray)
        {
          pending.Push(type.GetElementType()!);
          continue;
        }

        if (type.IsGenericType)
        {
          foreach (var argument in type.GetGenericArguments())
          {
            pending.Push(argument);
          }
        }

        if (type.Assembly != raygunAssembly || !type.IsClass || !found.Add(type))
        {
          continue;
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
          pending.Push(property.PropertyType);
        }
      }

      return found;
    }
  }
}
#endif
