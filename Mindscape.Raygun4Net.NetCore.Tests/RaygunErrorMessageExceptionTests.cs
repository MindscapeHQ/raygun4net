using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
    [TestFixture]
    public class RaygunErrorMessageExceptionTests
    {
        private Exception _exception;

        [SetUp]
        public void SetUp()
        {
            try
            {
                ExceptionallyCrappyMethod<string, int>("bogus");
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        private void ExceptionallyCrappyMethod<T, T2>(T bung)
        {
            throw new InvalidOperationException();
        }

        [Test]
        public void ExceptionBuilds()
        {
            Assert.That(() => RaygunErrorMessageBuilder.New().Build(_exception), Throws.Nothing);
        }

        [Test]
        public void FormatGenericExceptionClassName()
        {
            var message = RaygunErrorMessageBuilder.New().Build(new GenericException<Dictionary<string, List<object>>>());
            Assert.AreEqual("Mindscape.Raygun4Net.NetCore.Tests.GenericException<Dictionary<String,List<Object>>>", message.ClassName);
        }

        [Test]
        public void IncludeNamespaceInExceptionClassName()
        {
            var message = RaygunErrorMessageBuilder.New().Build(_exception);
            Assert.AreEqual("System.InvalidOperationException", message.ClassName);
        }
    }
}
