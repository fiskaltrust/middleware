using FluentAssertions;
using fiskaltrust.ifPOS.v1.de;
using NUnit.Framework;

namespace fiskaltrust.ifPOS.Tests.v1.IDESSCD
{
    public abstract class IDESSCDTests
    {
        protected abstract void StartHost();
        protected abstract void StopHost();
        protected abstract ifPOS.v1.de.IDESSCD CreateClient();

        [OneTimeSetUp]
        public void BaseSetUp() => StartHost();

        [OneTimeTearDown]
        public void BaseTearDown() => StopHost();

        [Test]
        public void StartTransactionAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.StartTransactionAsync(new StartTransactionRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void UpdateTransactionAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.UpdateTransactionAsync(new UpdateTransactionRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void FinishTransactionAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.FinishTransactionAsync(new FinishTransactionRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void GetTseInfoAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.GetTseInfoAsync().Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void SetTseStateAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.SetTseStateAsync(new TseState()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void RegisterClientId_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.RegisterClientIdAsync(new RegisterClientIdRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void UnregisterClientId_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.UnregisterClientIdAsync(new UnregisterClientIdRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void ExecuteSetTseTimeAsync_ShouldReturn()
        {
            var client = CreateClient();
            client.ExecuteSetTseTimeAsync().Wait();
        }

        [Test]
        public void ExecuteSelfTestAsync_ShouldReturn()
        {
            var client = CreateClient();
            client.ExecuteSelfTestAsync().Wait();
        }

        [Test]
        public void StartExportSessionAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.StartExportSessionAsync(new StartExportSessionRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void StartExportSessionByTimeStampAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.StartExportSessionByTimeStampAsync(new StartExportSessionByTimeStampRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void StartExportSessionByTransactionAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.StartExportSessionByTransactionAsync(new StartExportSessionByTransactionRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void ExportDataAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.ExportDataAsync(new ExportDataRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void EndExportSessionAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.EndExportSessionAsync(new EndExportSessionRequest()).Result;
            result.Should().NotBeNull();
        }
    }
}