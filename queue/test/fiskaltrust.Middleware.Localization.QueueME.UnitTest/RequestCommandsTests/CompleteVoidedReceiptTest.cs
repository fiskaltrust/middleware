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
            var (posReceiptCommand, _, scu) = await new PosReceiptCommandTests().InitializePosReceipt(queueItem, new InMemoryJournalMERepository(), new InMemoryActionJournalRepository(), queueMe).ConfigureAwait(false);
            var receiptToCancel = CreatePosReceiptToCancel();
            var inMemoryMesscd = new InMemoryMESSCD(scu.TcrCode, "iic", "iicSignature");
            var response =  await posReceiptCommand.ExecuteAsync(inMemoryMesscd, queue, receiptToCancel, queueItem, queueMe).ConfigureAwait(false);



        }

        private static ReceiptRequest CreateCompleteCancelReceipt()
        {
            return null;
            /*
            {
                "ftCashBoxID": "{{cashbox_id}}",
                "ftPosSystemId": "{{possystem_id}}",
                "cbTerminalID": "T2",
                "cbReceiptReference":137,
                "cbCustomer":"",
                "ftReceiptCaseData": "{'OperatorCode':'ir524mw732'}",
                "cbReceiptMoment":"{{current_moment}}",
                "cbChargeItems": [],
                "cbPayItems": [],
                "ftReceiptCase": 5567856514313748481,
                "cbPreviousReceiptReference":136
            }*/
        }

        private static ReceiptRequest CreatePosReceiptToCancel()
        {
            return new ReceiptRequest
            {
                cbReceiptReference = "140",
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
