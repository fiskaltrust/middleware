using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.it;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Contracts.Interfaces;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueIT.v2;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueIT.UnitTest
{
    public class QueueITStateTests
    {
        private static readonly Guid _queueID = new Guid();

        private readonly ftQueue _queue = new ftQueue
        {
            ftQueueId = _queueID,
        };

        private readonly ftQueue _queueStarted = new ftQueue
        {
            ftQueueId = _queueID,
            StartMoment = DateTime.UtcNow,
            ftReceiptNumerator = 1
        };

        private readonly ftQueue _queueStopped = new ftQueue
        {
            ftQueueId = _queueID,
            StartMoment = DateTime.UtcNow,
            StopMoment = DateTime.UtcNow
        };

        private IMarketSpecificSignProcessor GetSUT()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton(Mock.Of<IConfigurationRepository>(MockBehavior.Strict));
            serviceCollection.AddSingleton(Mock.Of<IJournalITRepository>());
            serviceCollection.AddSingleton(Mock.Of<IClientFactory<IITSSCD>>(MockBehavior.Strict));
            serviceCollection.AddSingleton(Mock.Of<IMiddlewareQueueItemRepository>(MockBehavior.Strict));
            serviceCollection.AddSingleton(new MiddlewareConfiguration());

            var bootstrapper = new QueueITBootstrapper();
            bootstrapper.ConfigureServices(serviceCollection);

            return serviceCollection.BuildServiceProvider().GetRequiredService<IMarketSpecificSignProcessor>();
        }

        public static IEnumerable<object[]> allNonInitialOperationReceipts()
        {
            foreach (var number in Enum.GetValues(typeof(ReceiptCases)))
            {
                if ((long) number == (long) ReceiptCases.InitialOperationReceipt0x4001)
                {
                    continue;
                }

                yield return new object[] { number };
            }
        }

        public static IEnumerable<object[]> allReceipts()
        {
            foreach (var number in Enum.GetValues(typeof(ReceiptCases)))
            {
                yield return new object[] { number };
            }
        }

        [Fact]
        public async Task DoNotAllowInitialOperationDuringInitializedState()
        {
            var initOperationReceipt = $$"""
 {
     "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
     "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
     "cbTerminalID": "00010001",
     "cbReceiptReference": "{{Guid.NewGuid()}}",
     "cbReceiptMoment": "{{DateTime.UtcNow.ToString("o")}}",
     "cbChargeItems": [],
     "cbPayItems": [],
     "ftReceiptCase": {{0x4954200000000000 | (long) ReceiptCases.InitialOperationReceipt0x4001}},
     "ftReceiptCaseData": "",
     "cbUser": "Admin"
 }
 """;
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(initOperationReceipt);
            var sut = GetSUT();
            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, _queueStarted, new ftQueueItem { });

            receiptResponse.ftSignatures.Should().HaveCount(1);
            receiptResponse.ftSignatures[0].Data.Should().Be($"The queue is already operational. It is not allowed to send another InitOperation Receipt");
            receiptResponse.ftState.Should().Be(0x4954_2000_EEEE_EEEE);
        }

        [Theory]
        [MemberData(nameof(allNonInitialOperationReceipts))]
        public async Task AllNonInitialOperationReceiptCases_ShouldReturnDisabledMessage_IfQueueHasNotStarted(ReceiptCases receiptCase)
        {
            var initOperationReceipt = $$"""
 {
     "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
     "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
     "cbTerminalID": "00010001",
     "cbReceiptReference": "{{Guid.NewGuid()}}",
     "cbReceiptMoment": "{{DateTime.UtcNow.ToString("o")}}",
     "cbChargeItems": [],
     "cbPayItems": [],
     "ftReceiptCase": {{0x4954200000000000 | (long) receiptCase}},
     "ftReceiptCaseData": "",
     "cbUser": "Admin"
 }
 """;
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(initOperationReceipt);
            var sut = GetSUT();
            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, _queue, new ftQueueItem { });

            receiptResponse.ftSignatures.Should().BeEmpty();
            receiptResponse.ftState.Should().Be(0x4954_2000_0000_0001);

            actionJournals.Should().HaveCount(1);
            actionJournals[0].Message.Should().Be($"QueueId {_queue.ftQueueId} has not been activated yet.");
        }

        [Theory]
        [MemberData(nameof(allReceipts))]
        public async Task AllReceiptCases_ShouldReturnDisabledMessage_IfQueueIsDeactivated(ReceiptCases receiptCase)
        {
            var initOperationReceipt = $$"""
 {
     "ftCashBoxID": "00000000-0000-0000-0000-000000000000",
     "ftPosSystemId": "00000000-0000-0000-0000-000000000000",
     "cbTerminalID": "00010001",
     "cbReceiptReference": "{{Guid.NewGuid()}}",
     "cbReceiptMoment": "{{DateTime.UtcNow.ToString("o")}}",
     "cbChargeItems": [],
     "cbPayItems": [],
     "ftReceiptCase": {{0x4954200000000000 | (long) receiptCase}},
     "ftReceiptCaseData": "",
     "cbUser": "Admin"
 }
 """;
            var receiptRequest = JsonConvert.DeserializeObject<ReceiptRequest>(initOperationReceipt);
            var sut = GetSUT();
            var (receiptResponse, actionJournals) = await sut.ProcessAsync(receiptRequest, _queueStopped, new ftQueueItem { });

            receiptResponse.ftSignatures.Should().BeEmpty();
            receiptResponse.ftState.Should().Be(0x4954_2000_0000_0001);

            actionJournals.Should().HaveCount(1);
            actionJournals[0].Message.Should().Be($"QueueId {_queue.ftQueueId} has been disabled.");
        }
    }
}
