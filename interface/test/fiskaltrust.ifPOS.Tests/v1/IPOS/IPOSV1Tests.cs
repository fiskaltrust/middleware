using System;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO;

namespace fiskaltrust.ifPOS.Tests.v1.IPOS
{
    public abstract class IPOSV1Tests
    {
        protected abstract void StartHost();
        protected abstract void StopHost();
        protected abstract ifPOS.v1.IPOS CreateClient();

        [OneTimeSetUp]
        public void BaseSetUp() => StartHost();

        [OneTimeTearDown]
        public void BaseTearDown() => StopHost();

        [Test]
        public async Task SignAsync_ShouldReturnSameQueueId()
        {
            var client = CreateClient();
            var queueId = Guid.NewGuid().ToString();
            var signRequest = new ReceiptRequest
            {
                ftQueueID = queueId
            };
            var response = await client.SignAsync(signRequest);
            response.ftQueueID.Should().Be(queueId);
        }

        [Test]
        public async Task EchoAsync_ShouldReturnSameMessage()
        {
            var client = CreateClient();
            var echoRequest = new EchoRequest
            {
                Message = "Hello World!"
            };
            var outMessage = await client.EchoAsync(echoRequest);
            outMessage.Message.Should().Be(echoRequest.Message);
        }
    }
}