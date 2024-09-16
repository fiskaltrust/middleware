﻿using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v1.de;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;


namespace fiskaltrust.Middleware.SCU.DE.SwissbitCloudV2.IntegrationTest
{
    [Collection("SwissbitCloudV2Tests")]
    public class SwissbitCloudV2Tests : IClassFixture<SwissbitCloudV2Fixture>
    {
        private readonly SwissbitCloudV2Fixture _testFixture;

        public SwissbitCloudV2Tests(SwissbitCloudV2Fixture testFixture)
        {
            _testFixture = testFixture;
        }
                
        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Return_Valid_Transaction_Result()
        {
            var sut = await _testFixture.GetSut();

            var request = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var result = await sut.StartTransactionAsync(request);

            result.Should().NotBeNull();
            result.TransactionNumber.Should().BeGreaterThan(0);
            result.SignatureData.Should().NotBeNull();
            result.SignatureData.SignatureBase64.Should().NotBeNull();
            result.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            result.ClientId.Should().Be(_testFixture.TestClientId.ToString());
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartTransactionAsync_Should_Fail_Because_ClientNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var ClientId = Guid.NewGuid().ToString();
            var request = CreateStartTransactionRequest(ClientId);
            var result = new Func<Task>(async () => await sut.StartTransactionAsync(request));

            await result.Should().ThrowAsync<Exception>().WithMessage($"The client {ClientId} is not registered.");
        }


        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task RegisterClientIdAsync_Should_Register_NewClient()
        {
            var sut = await _testFixture.GetSut();
            var ClientId = Guid.NewGuid().ToString().Replace("-","").Remove(30);

            var clients = await sut.RegisterClientIdAsync(new RegisterClientIdRequest
            {
                ClientId = ClientId
            });
            clients.ClientIds.Should().Contain(ClientId);           
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UnregisterClientIdAsync_Should_Remove_Registered_Client()
        {
            var sut = await _testFixture.GetSut();

            var ClientId = Guid.NewGuid().ToString().Replace("-", "").Remove(30);
            await sut.RegisterClientIdAsync(new RegisterClientIdRequest { ClientId = ClientId });

            var clientsBeforeUnregister = await sut.RegisterClientIdAsync(new RegisterClientIdRequest { ClientId = ClientId });
            clientsBeforeUnregister.ClientIds.Should().Contain(ClientId);

            await sut.UnregisterClientIdAsync(new UnregisterClientIdRequest { ClientId = ClientId });

            var clientsAfterUnregister = await sut.RegisterClientIdAsync(new RegisterClientIdRequest { ClientId = ClientId });
            clientsAfterUnregister.ClientIds.Should().NotContain(ClientId);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Not_Increment_TransactionNumber_And_Increment_SignatureCounter()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var updateRequest = CreateUpdateTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var updateResult = await sut.UpdateTransactionAsync(updateRequest);

            updateResult.Should().NotBeNull();
            updateResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            updateResult.SignatureData.Should().NotBeNull();
            updateResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            updateResult.SignatureData.SignatureBase64.Should().NotBeNull();
            updateResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            updateResult.ProcessDataBase64.Should().BeEquivalentTo(updateRequest.ProcessDataBase64);
            updateResult.ProcessType.Should().Be(updateRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task UpdateTransactionAsync_Should_Fail_Because_ClientIdIsNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var clientId = Guid.NewGuid().ToString();
            var request = CreateUpdateTransactionRequest(0, clientId);
            var action = new Func<Task>(async () => await sut.UpdateTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {clientId} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Not_Increment_TransactionNumber()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.SignatureData.SignatureBase64.Should().NotBeNull();
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);

            var tseInfo = await sut.GetTseInfoAsync();
            //startResult.TseSerialNumberOctet.Should().Be(tseInfo.SerialNumberOctet);
            //finishResult.TseSerialNumberOctet.Should().Be(tseInfo.SerialNumberOctet);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task StartAndFinishTransactionAsync_Should_WorkFor_OrderWithoutContent()
        {
            var sut = await _testFixture.GetSut();

            var startRequest = CreateOrderStartTransactionRequest(_testFixture.TestClientId.ToString(), "");
            var startResult = await sut.StartTransactionAsync(startRequest);

            var finishRequest = CreateOrderFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId, "");
            var finishResult = await sut.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.SignatureData.SignatureBase64.Should().NotBeNull();
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Fail_Because_ClientIdIsNotRegistered()
        {
            var sut = await _testFixture.GetSut();
            var serialNumber = Guid.NewGuid().ToString();
            var request = CreateFinishTransactionRequest(0, serialNumber);
            var action = new Func<Task>(async () => await sut.FinishTransactionAsync(request));

            await action.Should().ThrowAsync<Exception>().WithMessage($"The client {serialNumber} is not registered.");
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task FinishTransactionAsync_Should_Succeed_EvenIf_MemoryIsLost()
        {
            var sut = await _testFixture.GetSut();
            var startRequest = CreateStartTransactionRequest(_testFixture.TestClientId.ToString());
            var startResult = await sut.StartTransactionAsync(startRequest);

            // We do simulate a restart of the SCU. If the SCU is restarted we loose all state and so this is similar 
            // as if we recreate the sut.
            var sut2 = _testFixture.GetNewSut();
            var finishRequest = CreateFinishTransactionRequest(startResult.TransactionNumber, startRequest.ClientId);
            var finishResult = await sut2.FinishTransactionAsync(finishRequest);

            finishResult.Should().NotBeNull();
            finishResult.TransactionNumber.Should().Be(startResult.TransactionNumber);
            finishResult.SignatureData.Should().NotBeNull();
            finishResult.SignatureData.SignatureCounter.Should().BeGreaterThan(0);
            finishResult.ClientId.Should().Be(_testFixture.TestClientId.ToString());
            finishResult.ProcessDataBase64.Should().BeEquivalentTo(finishRequest.ProcessDataBase64);
            finishResult.ProcessType.Should().Be(finishRequest.ProcessType);
        }

        [Fact]
        [Trait("TseCategory", "Cloud")]
        public async Task GetTseInfoAsync_Should_Return_Valid_TseInfo()
        {
            var sut = await _testFixture.GetSut();

            var tseInfo = await sut.GetTseInfoAsync();

            tseInfo.Should().NotBeNull();
            tseInfo.SerialNumber.Should().NotBeNullOrEmpty();

            var expectedHealthStates = new[] { TseHealthState.Started, TseHealthState.Stopped, TseHealthState.Defect };
            expectedHealthStates.Should().Contain(tseInfo.HealthState);

            var expectedInitStates = new[] { TseInitializationState.Initialized, TseInitializationState.Uninitialized, TseInitializationState.Disabled };
            expectedInitStates.Should().Contain(tseInfo.InitializationState);
        }


        private Task GetSut()
        {
            return _testFixture.GetSut();  
        }

        private StartTransactionRequest CreateStartTransactionRequest(string clientId)
        {
            var fixture = new Fixture();
            return new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private UpdateTransactionRequest CreateUpdateTransactionRequest(ulong transactionNumber, string clientId)
        {
            var fixture = new Fixture();
            return new UpdateTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private FinishTransactionRequest CreateFinishTransactionRequest(ulong transactionNumber, string clientId)
        {
            var fixture = new Fixture();
            return new FinishTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = Convert.ToBase64String(fixture.CreateMany<byte>(100).ToArray()),
                ProcessType = "Kassenbeleg-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private StartTransactionRequest CreateOrderStartTransactionRequest(string clientId, string processDataBase64)
        {
            return new StartTransactionRequest
            {
                ClientId = clientId,
                ProcessDataBase64 = processDataBase64,
                ProcessType = "",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

        private FinishTransactionRequest CreateOrderFinishTransactionRequest(ulong transactionNumber, string clientId, string processDataBase64)
        {
            return new FinishTransactionRequest
            {
                TransactionNumber = transactionNumber,
                ClientId = clientId,
                ProcessDataBase64 = processDataBase64,
                ProcessType = "Bestellung-V1",
                QueueItemId = Guid.NewGuid(),
                IsRetry = false,
            };
        }

       
    }
}