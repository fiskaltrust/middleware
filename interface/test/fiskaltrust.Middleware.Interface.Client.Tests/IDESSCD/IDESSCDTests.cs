using System;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
using NUnit.Framework;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;

namespace fiskaltrust.Middleware.Interface.Client.Tests.IDESSCD
{
    public abstract class IDESSCDTests
    {
        protected RetryPolicyOptions _retryPolicyOptions = new RetryPolicyOptions
        {
            Retries = 3,
            DelayBetweenRetries = TimeSpan.FromSeconds(7),
            ClientTimeout = TimeSpan.FromSeconds(15)
        };

        protected abstract void StartHost();
        protected abstract void StopHost();
        protected abstract ifPOS.v1.de.IDESSCD CreateClient();

        [SetUp]
        public void BaseSetUp() => StartHost();

        [OneTimeTearDown]
        public void BaseTearDown() => StopHost();

        [Test]
        public void ExportDataAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.ExportDataAsync(new ExportDataRequest()).Result;
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
        public void SetTseStateAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.SetTseStateAsync(new TseState()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void EchoAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.EchoAsync(new ScuDeEchoRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void EndExportSessionAsync_ShouldReturn()
        {
            var client = CreateClient();
            var result = client.EndExportSessionAsync(new EndExportSessionRequest()).Result;
            result.Should().NotBeNull();
        }

        [Test]
        public void ExecuteSelfTestAsync_ShouldNotThrow()
        {
            var client = CreateClient();
            client.ExecuteSelfTestAsync().Wait();
        }

        [Test]
        public void ExecuteSetTseTimeAsync_ShouldNotThrow()
        {
            var client = CreateClient();
            client.ExecuteSetTseTimeAsync().Wait();
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
            var result = client.StartExportSessionByTimeStampAsync(new StartExportSessionByTimeStampRequest { From = DateTime.Now, To = DateTime.Now }).Result;
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
        public async Task GetTseInfoAsync_ShouldHandleRetry_EvenIf_ServerShutDown()
        {
            var finished = false;
            var client = CreateClient();
            var tseInfoThread = Task.Run(async () =>
            {
                while (!finished)
                {
                    var result = await client.GetTseInfoAsync();
                    result.Should().NotBeNull();
                }
            });

            StopHost();
            await Task.Delay(TimeSpan.FromSeconds(3));
            StartHost();
            finished = true;
            await tseInfoThread;
        }

        [Test]
        public async Task GetTseInfoAsync_ShouldHandleRetry_EvenIf_ServerShutDown_ThrowsException()
        {
            var finished = false;
            var client = CreateClient();
            StopHost();

            var tseInfoThread = Task.Run(async () =>
            {
                while (!finished)
                {
                    Func<Task<TseInfo>> act = async () => await client.GetTseInfoAsync();
                    await act.Should().ThrowAsync<Exception>();
                }
            });

            await Task.Delay(TimeSpan.FromSeconds(10));
            finished = true;

            await tseInfoThread;
        }
    }
}