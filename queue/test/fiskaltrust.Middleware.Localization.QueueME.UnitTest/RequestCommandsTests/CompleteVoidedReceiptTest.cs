using System;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.me;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueME;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class CompleteVoidedReceiptTest
    {
        [Fact]
        public async Task ExecuteAsync_CompleteVoidedReceipt_ValidResultAsync()
        {
            var businessUnitCode = "abc1234";
            var issuerTin = "12345";
            var queue = new ftQueue
            {
                ftQueueId = Guid.NewGuid()
            };
            var queueMe = new ftQueueME
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var queueItem = new ftQueueItem { ftQueueItemId = Guid.NewGuid(), ftWorkMoment = DateTime.UtcNow };
            var (posReceiptCommand, scu) = await new PosReceiptCommandTests().CreateSut(queueItem, new InMemoryJournalMERepository(), new InMemoryActionJournalRepository(), queueMe,
                queue.ftQueueId.ToString(), businessUnitCode, issuerTin);
            var receiptToCancel = CreatePosReceiptToCancel();
            var inMemoryMesscd = new InMemoryMESSCD(scu.TcrCode, "iic", "iicSignature");
            var response = await posReceiptCommand.ExecuteAsync(inMemoryMesscd, queue, receiptToCancel, queueItem, queueMe);
        }

        private static ReceiptRequest CreatePosReceiptToCancel()
        {
            return new ReceiptRequest
            {
                cbReceiptReference = "140",
                cbUser = "{\"OperatorCode\": \"abc\"}",
                cbCustomer =
                    "{'BuyerIdentificationType':'TIN','IdentificationNumber':'72001008','Name':'Mr. X','Address':'Mustergasse 8','Town':'City','Country':'MNE'}",
                ftReceiptCaseData = "{'OperatorCode':'ir524mw732'}",
                cbReceiptMoment = DateTime.UtcNow,
                cbChargeItems = new[]
                {
                    new ChargeItem
                    {
                        Quantity = 2,
                        Amount = 221,
                        ProductBarcode = "Testbarcode1",
                        Unit = "piece",
                        UnitPrice = 110.5m,
                        VATRate = 21,
                        Description = "TestChargeItem1",
                        ftChargeItemCase = 5567856514313486337,
                        Moment = DateTime.UtcNow
                    },
                    new ChargeItem
                    {
                        Quantity = 1,
                        Amount = 107,
                        ftChargeItemCase = 5567856514313486337,
                        ProductBarcode = "Testbarcode2",
                        Unit = "piece",
                        UnitPrice = 107,
                        VATRate = 7,
                        Description = "TestChargeItem2",
                        Moment = DateTime.UtcNow
                    }
                },
                cbPayItems = new[]
                {
                    new PayItem
                    {
                        Quantity = 1,
                        Description = "Cash",
                        ftPayItemCase = 5567856514313486337,
                        Moment = DateTime.UtcNow,
                        Amount = 328
                    }
                },
                ftReceiptCase = 5567856514313486337
            };
        }
    }
}
