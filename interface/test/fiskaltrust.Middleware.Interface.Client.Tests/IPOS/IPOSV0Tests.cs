using System;
using FluentAssertions;
using fiskaltrust.ifPOS.v0;
using NUnit.Framework;
using System.Threading.Tasks;

namespace fiskaltrust.Middleware.Interface.Client.Tests.IPOS
{
    public abstract class IPOSV0Tests
    {
        protected abstract void StartHost();
        protected abstract void StopHost();
        protected abstract ifPOS.v0.IPOS CreateClient();

        [OneTimeSetUp]
        public void BaseSetUp() => StartHost();

        [OneTimeTearDown]
        public void BaseTearDown() => StopHost();

        [Test]
        [Obsolete]
        public void Sign_ShouldReturnSameQueueId()
        {
            var client = CreateClient();
            var queueId = Guid.NewGuid().ToString();
            var signRequest = new ReceiptRequest
            {
                ftQueueID = queueId
            };
            var response = client.Sign(signRequest);
            response.ftQueueID.Should().Be(queueId);
        }

        [Test]
        [Obsolete]
        public void Echo_ShouldReturnSameMessage()
        {
            var client = CreateClient();
            var message = "Hello World!";
            var outMessage = client.Echo(message);
            outMessage.Should().Be(message);
        }
    }
}