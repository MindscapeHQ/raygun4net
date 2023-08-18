using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Mindscape.Raygun4Net.NetCore.Tests
{
    [TestFixture]
    public class RaygunClientBaseTests
    {
        private FakeRaygunClient _client = new FakeRaygunClient();

        [Test]
        public void FlagAsSentDoesNotCrash_DataDoesNotSupportStringKeys()
        {
            Assert.That(() => _client.ExposeFlagAsSent(new FakeException(new Dictionary<int, object>())), Throws.Nothing);
        }

        [Test]
        public void FlagAsSentDoesNotCrash_NullData()
        {
            Assert.That(() => _client.ExposeFlagAsSent(new FakeException(null)), Throws.Nothing);
        }

        [Test]
        public void CanSendIfDataIsNull()
        {
            Assert.IsTrue(_client.ExposeCanSend(new FakeException(null)));
        }

        [Test]
        public void CannotSendSentException_StringDictionary()
        {
            Exception exception = new FakeException(new Dictionary<string, object>());
            _client.ExposeFlagAsSent(exception);
            Assert.IsFalse(_client.ExposeCanSend(exception));
        }

        [Test]
        public void CannotSendSentException_ObjectDictionary()
        {
            Exception exception = new FakeException(new Dictionary<object, object>());
            _client.ExposeFlagAsSent(exception);
            Assert.IsFalse(_client.ExposeCanSend(exception));
        }

        [Test]
        public void ExceptionInsideSendingMessageHAndlerDoesNotCrash()
        {
            FakeRaygunClient client = new FakeRaygunClient();
            client.SendingMessage += (sender, args) =>
            {
                throw new Exception("Oops...");
            };

            Assert.That(() => client.ExposeOnSendingMessage(new RaygunMessage()), Throws.Nothing);
            Assert.IsTrue(client.ExposeOnSendingMessage(new RaygunMessage()));
        }
    }
}
