using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1.de;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.InMemory.UnitTest
{
    public class InMemorySCUTests
    {
        private const string ProcessData = "This is a Testpost of client: ";

        [Fact]
        public async Task TestStartTransactionAsync_startTransaction_ValidResultAsync()
        {
            var clientId = Guid.NewGuid().ToString();
            var queueItemId = Guid.NewGuid();
            var StartTransactionRequest = new StartTransactionRequest()
            {
                ClientId = clientId,
                QueueItemId = queueItemId

            };
            IDESSCD dESSCD = CreateInMemorySCU();
            var transaction = await dESSCD.StartTransactionAsync(StartTransactionRequest).ConfigureAwait(false);
            transaction.Should().NotBeNull();
            transaction.ClientId.Should().Be(clientId);
            transaction.SignatureData.Should().NotBeNull();
        }

        [Fact]
        public void TestStartTransactionAsync_ClientIdNull_ArgumentNullException()
        {
            var clientId = Guid.NewGuid().ToString();
            var queueItemId = Guid.NewGuid();
            var startTransactionRequest = new StartTransactionRequest();
            IDESSCD dESSCD = CreateInMemorySCU();
            Func<Task> f = async () => await dESSCD.StartTransactionAsync(startTransactionRequest).ConfigureAwait(false);
            f.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task TestUpdateTransactionAsync_UpdateTransaction_ValidResult()
        {
            var clientId = Guid.NewGuid().ToString();
            var queueItemId = Guid.NewGuid();
            var processText = ProcessData +  clientId;

            var StartTransactionRequest = new StartTransactionRequest()
            {
                ClientId = clientId,
                QueueItemId = queueItemId
            };
            var inMemorySCU = CreateInMemorySCU();
            var transaction = await inMemorySCU.StartTransactionAsync(StartTransactionRequest).ConfigureAwait(false);
            var updateTransactionRequest = new UpdateTransactionRequest()
            {
                ClientId = clientId,
                QueueItemId = queueItemId,
                TransactionNumber = transaction.TransactionNumber,
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(processText))
            };
            var updatedtransaction = await inMemorySCU.UpdateTransactionAsync(updateTransactionRequest).ConfigureAwait(false);
            updatedtransaction.Should().NotBeNull();
            updatedtransaction.ClientId.Should().Be(clientId);
            updatedtransaction.SignatureData.Should().NotBeNull();
            var rawdata = DecodeBase64(updatedtransaction.ProcessDataBase64);
            rawdata.Should().Contain(clientId);
            var lastRevisionForTransaction = await inMemorySCU.GetTransactionStateByNumberAsync(updatedtransaction.TransactionNumber).ConfigureAwait(false);
            lastRevisionForTransaction.Should().Be(2);
            updatedtransaction.TransactionNumber.Should().Be(transaction.TransactionNumber);
        }

        [Fact]
        public async Task TestFinishTransactionAsync_FinischTransaction_ValidResult()
        {
            var clientId = Guid.NewGuid().ToString();
            var queueItemId = Guid.NewGuid();
            var processText = ProcessData + clientId;

            var StartTransactionRequest = new StartTransactionRequest()
            {
                ClientId = clientId,
                QueueItemId = queueItemId
            };
            var inMemorySCU = CreateInMemorySCU();
            var transaction = await inMemorySCU.StartTransactionAsync(StartTransactionRequest).ConfigureAwait(false);
            var finishTransactionRequest = new FinishTransactionRequest()
            {
                ClientId = clientId,
                QueueItemId = queueItemId,
                TransactionNumber = transaction.TransactionNumber,
                ProcessDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(processText))
            };
            var finishtransaction = await inMemorySCU.FinishTransactionAsync(finishTransactionRequest).ConfigureAwait(false);
            finishtransaction.Should().NotBeNull();
            finishtransaction.SignatureData.Should().NotBeNull();
            finishtransaction.ClientId.Should().Be(clientId);
            finishtransaction.TransactionNumber.Should().Be(transaction.TransactionNumber);
            var rawdata = DecodeBase64(finishtransaction.ProcessDataBase64);
            rawdata.Should().Contain(clientId);
        }

        [Fact]
        public async Task TestGetTseInfoAsync_CompareInmemoryTseInfo()
        {
            var inMemorySCU = CreateInMemorySCU();
            var tseInfo = await inMemorySCU.GetTseInfoAsync().ConfigureAwait(false);
            using (new AssertionScope())
            {
                tseInfo.MaxNumberOfClients.Should().Be(int.MaxValue);
                tseInfo.MaxNumberOfStartedTransactions.Should().Be(int.MaxValue);
                tseInfo.MaxNumberOfSignatures.Should().Be(long.MaxValue);
                tseInfo.MaxLogMemorySize.Should().Be(long.MaxValue);
                tseInfo.FirmwareIdentification.Should().Be(InMemoryTSE.TssResultVersion);
                tseInfo.CertificationIdentification.Should().Be(InMemoryTSE.CertificationIdentification);
                tseInfo.SignatureAlgorithm.Should().Be(InMemoryTSE.SignatureAlgorithm);
                tseInfo.LogTimeFormat.Should().Be(InMemoryTSE.LogTimeFormat);
                tseInfo.SerialNumberOctet.Should().Be(InMemoryTSE.TssCertificateSerial);
                tseInfo.PublicKeyBase64.Should().Be(InMemoryTSE.TssPublicKey);
                tseInfo.CertificatesBase64.ToList().First().Should().BeEquivalentTo(Convert.ToBase64String(Encoding.UTF8.GetBytes(InMemoryTSE.Certificate)));
                tseInfo.CurrentLogMemorySize.Should().Be(-1);
                tseInfo.CurrentNumberOfSignatures.Should().Be(0);
                tseInfo.CurrentNumberOfStartedTransactions.Should().Be(0);
                tseInfo.CurrentState.Should().Be(TseStates.Initialized);
                tseInfo.CurrentNumberOfClients.Should().Be(0);
                tseInfo.CurrentClientIds.Count().Should().Be(0);
                tseInfo.Info.Should().BeNull();
            }
        }

        [Fact]
        public async Task TestGetTseInfoAsync_WithÓneStart_CompareInmemoryTseInfo()
        {
            var inMemorySCU = CreateInMemorySCU();
            var clientId = Guid.NewGuid().ToString();
            var queueItemId = Guid.NewGuid();
            var StartTransactionRequest = new StartTransactionRequest()
            {
                ClientId = clientId,
                QueueItemId = queueItemId

            };
            _ = await inMemorySCU.StartTransactionAsync(StartTransactionRequest).ConfigureAwait(false);
            var tseInfo = await inMemorySCU.GetTseInfoAsync().ConfigureAwait(false);
            using (new AssertionScope())
            {
                tseInfo.MaxNumberOfClients.Should().Be(int.MaxValue);
                tseInfo.MaxNumberOfStartedTransactions.Should().Be(int.MaxValue);
                tseInfo.MaxNumberOfSignatures.Should().Be(long.MaxValue);
                tseInfo.MaxLogMemorySize.Should().Be(long.MaxValue);
                tseInfo.FirmwareIdentification.Should().Be(InMemoryTSE.TssResultVersion);
                tseInfo.CertificationIdentification.Should().Be(InMemoryTSE.CertificationIdentification);
                tseInfo.SignatureAlgorithm.Should().Be(InMemoryTSE.SignatureAlgorithm);
                tseInfo.LogTimeFormat.Should().Be(InMemoryTSE.LogTimeFormat);
                tseInfo.SerialNumberOctet.Should().Be(InMemoryTSE.TssCertificateSerial);
                tseInfo.PublicKeyBase64.Should().Be(InMemoryTSE.TssPublicKey);
                tseInfo.CertificatesBase64.ToList().First().Should().BeEquivalentTo(Convert.ToBase64String(Encoding.UTF8.GetBytes(InMemoryTSE.Certificate)));
                tseInfo.CurrentLogMemorySize.Should().Be(-1);
                tseInfo.CurrentNumberOfSignatures.Should().Be(1);
                tseInfo.CurrentNumberOfStartedTransactions.Should().Be(1);
                tseInfo.CurrentState.Should().Be(TseStates.Initialized);
                tseInfo.CurrentNumberOfClients.Should().Be(1);
                tseInfo.CurrentClientIds.ToList().First().Should().BeEquivalentTo(clientId);
                tseInfo.Info.Should().BeNull();
            }
        }

        [Fact]
        public async Task TestSetTseStateAsync_SetState_ValidResult()
        {
            var inMemorySCU = CreateInMemorySCU();
            _ = await inMemorySCU.SetTseStateAsync(new TseState() { CurrentState = TseStates.Terminated }).ConfigureAwait(false);
            var tseInfo = await inMemorySCU.GetTseInfoAsync().ConfigureAwait(false);
            tseInfo.CurrentState.Should().Be(TseStates.Terminated);
        }

        [Fact]
        public async Task TestStartExportSessionAsync_Export_ValidResult()
        {
            var inMemorySCU = CreateInMemorySCU();
            var clientId = Guid.NewGuid().ToString();
            var startExportSessionResponse = await inMemorySCU.StartExportSessionAsync(new StartExportSessionRequest() { ClientId = clientId }).ConfigureAwait(false);
            startExportSessionResponse.TseSerialNumberOctet.Should().Be(InMemoryTSE.TssCertificateSerial);
        }

        [Fact]
        public async Task TestExportDataAsync_Export_ValidResult()
        {
            var inMemorySCU = CreateInMemorySCU();
            var TokenId = Guid.NewGuid().ToString();
            var exportDataResponse = await inMemorySCU.ExportDataAsync(new ExportDataRequest() { TokenId = TokenId }).ConfigureAwait(false);
            exportDataResponse.TokenId.Should().Be(TokenId);
            DecodeBase64(exportDataResponse.TarFileByteChunkBase64).Should().Be("This is a TestTarFile!");
        }

        [Fact]
        public async Task TestEndExportSessionAsync_EndExport_ValidResult()
        {
            var inMemorySCU = CreateInMemorySCU();
            var TokenId = Guid.NewGuid().ToString();
            var exportDataResponse = await inMemorySCU.EndExportSessionAsync(new EndExportSessionRequest() { TokenId = TokenId }).ConfigureAwait(false);
            exportDataResponse.TokenId.Should().Be(TokenId);
        }

        [Fact]
        public async Task TestEndExportSessionAsync_EchoAsync_ValidResult()
        {
            var inMemorySCU = CreateInMemorySCU();
            var message = "InMemoryTestMessage";
            var exportDataResponse = await inMemorySCU.EchoAsync(new ScuDeEchoRequest() { Message = message }).ConfigureAwait(false);
            exportDataResponse.Message.Should().Be(message);
        }

        [Fact]
        public async Task TestRegisterClientIdAsync_Register_ValidResult()
        {
            var inMemorySCU = CreateInMemorySCU();
            var clientId = Guid.NewGuid().ToString();
            var registerClientIdResponse = await inMemorySCU.RegisterClientIdAsync(new RegisterClientIdRequest() { ClientId = clientId }).ConfigureAwait(false);
            registerClientIdResponse.ClientIds.First().Should().Be(clientId);
        }

        [Fact]
        public async Task TestUnRegisterClientIdAsync_RegisterAndUnregister_ValidResult()
        {
            var inMemorySCU = CreateInMemorySCU();
            var clientId = Guid.NewGuid().ToString();
            var registerClientIdResponse = await inMemorySCU.RegisterClientIdAsync(new RegisterClientIdRequest() { ClientId = clientId }).ConfigureAwait(false);
            registerClientIdResponse.ClientIds.First().Should().Be(clientId);
            var unregisterClientIdResponse = await inMemorySCU.UnregisterClientIdAsync(new UnregisterClientIdRequest() { ClientId = clientId }).ConfigureAwait(false);
            unregisterClientIdResponse.ClientIds.Should().BeEmpty();
        }
        private InMemorySCU CreateInMemorySCU()
        {
            ILoggerFactory nullLoggerFactory = new NullLoggerFactory();
            return new InMemorySCU(nullLoggerFactory.CreateLogger<InMemorySCU>(), new InMemoryTSE());
        }
        private string DecodeBase64(string data)
        {
            var base64EncodedBytes = Convert.FromBase64String(data);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
