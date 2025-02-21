#if WCF

using System;
using System.IO;
using NUnit.Framework;
using FluentAssertions;

namespace fiskaltrust.ifPOS.Tests.v0.IPOS
{
    public abstract class IPOSTests
    {
        private Stream JournalAsyncStream = null;

        protected abstract void StartHost();
        protected abstract void StopHost();
        protected abstract ifPOS.v0.IPOS CreateClient();

        [OneTimeSetUp]
        public void BaseSetUp() => StartHost();

        [OneTimeTearDown]
        public void BaseTearDown() => StopHost();

        [Test]
        [Obsolete]
        public void Echo()
        {
            var client = CreateClient();
            string inMessage = "Hello World!";
            string outMessage = client.Echo(inMessage);
            outMessage.Should().Be(inMessage);
        }

        [Test]
        [Obsolete]
        public void EchoAsync()
        {
            var client = CreateClient();
            string echoAsyncMessage = null;
            string inMessage = "Hello World!";

            var ar = client.BeginEcho(inMessage, new AsyncCallback(result =>
            {
                var asyncState = (ifPOS.v0.IPOS)result.AsyncState;
                echoAsyncMessage = asyncState.EndEcho(result);
            }), client);
            while (echoAsyncMessage == null)
                System.Threading.Thread.Sleep(0);

            echoAsyncMessage.Should().Be(inMessage);
        }

        [Test]
        [Obsolete]
        public void Sign()
        {
            var client = CreateClient();
            var request = new ifPOS.v0.ReceiptRequest();
            var response = client.Sign(request);

            response.Should().NotBeNull();
        }
        private ifPOS.v0.ReceiptResponse SignAsyncResponse = null;
        [Test]
        [Obsolete]
        public void SignAsync()
        {
            var client = CreateClient();
            var request = new ifPOS.v0.ReceiptRequest();

            var ar = client.BeginSign(request, new AsyncCallback(result =>
            {
                var asyncState = (ifPOS.v0.IPOS)result.AsyncState;
                SignAsyncResponse = asyncState.EndSign(result);
            }), client);
            while (SignAsyncResponse == null)
                System.Threading.Thread.Sleep(0);

            SignAsyncResponse.Should().NotBeNull();
        }

        [Test]
        [Obsolete]
        public void Journal()
        {
            var client = CreateClient();
            Stream outStream = client.Journal(0x0, 0x0, 0x0);

            var ms = new MemoryStream();
            outStream.CopyTo(ms);
            ms.Length.Should().BeGreaterThan(0);
        }


        [Test]
        [Obsolete]
        public void JournalAsync()
        {
            var client = CreateClient();
            var ar = client.BeginJournal(0x0, 0x0, 0x0, new AsyncCallback(result =>
            {
                var asyncState = (ifPOS.v0.IPOS)result.AsyncState;
                JournalAsyncStream = asyncState.EndJournal(result);
            }), client);
            while (JournalAsyncStream == null)
                System.Threading.Thread.Sleep(0);

            var ms = new MemoryStream();
            JournalAsyncStream.CopyTo(ms);
            ms.Length.Should().BeGreaterThan(0);
        }
    }
}

#endif