using System;
using System.Threading.Tasks;
using System.Linq;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Localization.QueueME.Models;
using fiskaltrust.Middleware.Localization.QueueME.RequestCommands;
using fiskaltrust.Middleware.Localization.QueueME.UnitTest.Helper;
using fiskaltrust.Middleware.Storage.InMemory.Repositories;
using fiskaltrust.storage.V0;
using Newtonsoft.Json;
using Xunit;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.DE.MasterData;
using Moq;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Storage.InMemory.Repositories.ME;
using FluentAssertions;
using fiskaltrust.Middleware.Contracts.Constants;
using fiskaltrust.Middleware.Localization.QueueME.Exceptions;

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class PosReceiptCommandTests
    {

        [Fact]
        public async Task ExecuteAsync_RegisterInvoice_ValidResultAsync()
        {
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, Guid.NewGuid(), DateTime.UtcNow).ConfigureAwait(false);
            var posReceiptCommand = await InitializePosReceipt(queue, new ftQueueItem(), new InMemoryJournalMERepository(), inMemoryActionJournalRepository).ConfigureAwait(false);
            var receiptRequest = CreateReceiptRequest();
            var inMemoryMESSCD = new InMemoryMESSCD("TestTCRCodePos");
            var queueItem = new ftQueueItem()
            {
                ftWorkMoment = DateTime.Now
            };
            await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem).ConfigureAwait(false);

        }

        [Fact]
        public async Task ExecuteAsync_RegisterSecondInvoice_IncrementOrdNr()
        {
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var existingQueueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var journal = new ftJournalME()
            {
                ftQueueItemId = existingQueueItem.ftQueueItemId,
                ftQueueId = existingQueueItem.ftQueueId,
                ftOrdinalNumber = 8
            };
            await inMemoryJournalMERepository.InsertAsync(journal).ConfigureAwait(false);
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, Guid.NewGuid(), DateTime.UtcNow).ConfigureAwait(false);
            var posReceiptCommand = await InitializePosReceipt(queue, existingQueueItem, inMemoryJournalMERepository, inMemoryActionJournalRepository).ConfigureAwait(false);
            var receiptRequest = CreateReceiptRequest();
            var inMemoryMESSCD = new InMemoryMESSCD("TestTCRCodePos");
            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };
            await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem).ConfigureAwait(false);
            var journalMEs = await inMemoryJournalMERepository.GetAsync().ConfigureAwait(false);
            var journalME = journalMEs.Where(x => x.ftQueueItemId.Equals(queueItem.ftQueueItemId));
            Assert.Single(journalME);
            journalME.FirstOrDefault().ftOrdinalNumber.Should().Be(9);
        }

        [Fact]
        public async Task ExecuteAsync_CashDepositOutstanding_Exception()
        {
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var existingQueueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.UtcNow.AddDays(-1)
            };
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var journal = new ftJournalME()
            {
                ftQueueItemId = existingQueueItem.ftQueueItemId,
                ftQueueId = existingQueueItem.ftQueueId,
                ftOrdinalNumber = 8
            };
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, existingQueueItem.ftQueueItemId, DateTime.UtcNow.AddDays(-1)).ConfigureAwait(false);
            await inMemoryJournalMERepository.InsertAsync(journal).ConfigureAwait(false);
            var receiptRequest = CreateReceiptRequest();
            var posReceiptCommand = new PosReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(),
                new InMemoryConfigurationRepository(), inMemoryJournalMERepository, new InMemoryQueueItemRepository(), inMemoryActionJournalRepository);
            var sutMethod = CallInitialOperationReceiptCommand(posReceiptCommand, queue, receiptRequest);
            await sutMethod.Should().ThrowAsync<CashDepositOutstandingException>().ConfigureAwait(false);
        }

        private static async Task<InMemoryActionJournalRepository> IniActionJournalRepo(ftQueue queue, Guid ftQueueItemId, DateTime datetime)
        {
            var inMemoryActionJournalRepository = new InMemoryActionJournalRepository();
            var actionjounal = new ftActionJournal()
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = ftQueueItemId,
                Type = JournalTypes.CashDepositME.ToString(),
                Moment = datetime
            };
            await inMemoryActionJournalRepository.InsertAsync(actionjounal).ConfigureAwait(false);
            return inMemoryActionJournalRepository;
        }

        private Func<Task> CallInitialOperationReceiptCommand(PosReceiptCommand posReceiptCommand, ftQueue queue, ReceiptRequest receiptRequest)
        {
            return async () => { var receiptResponse = await posReceiptCommand.ExecuteAsync(new InMemoryMESSCD("testTcr"), queue, receiptRequest, new ftQueueItem()); };
        }


        [Fact]
        public async Task ExecuteAsync_RegisterInvoiceNextYear_ResetOrdNr()
        {
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var existingQueueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now.AddYears(-1)
            };
            var inMemoryJournalMERepository = new InMemoryJournalMERepository();
            var journal = new ftJournalME()
            {
                ftQueueItemId = existingQueueItem.ftQueueItemId,
                ftQueueId = existingQueueItem.ftQueueId,
                ftOrdinalNumber = 8
            };
            await inMemoryJournalMERepository.InsertAsync(journal).ConfigureAwait(false);
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, Guid.NewGuid(), DateTime.UtcNow).ConfigureAwait(false);
            var posReceiptCommand = await InitializePosReceipt(queue, existingQueueItem, inMemoryJournalMERepository, inMemoryActionJournalRepository).ConfigureAwait(false);
            var receiptRequest = CreateReceiptRequest();
            var inMemoryMESSCD = new InMemoryMESSCD("TestTCRCodePos");
            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };
            await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem).ConfigureAwait(false);
            var journalMEs = await inMemoryJournalMERepository.GetAsync().ConfigureAwait(false);
            var journalME = journalMEs.Where(x => x.ftQueueItemId.Equals(queueItem.ftQueueItemId));
            Assert.Single(journalME);
            journalME.FirstOrDefault().ftOrdinalNumber.Should().Be(1);
        }

        private async Task<PosReceiptCommand> InitializePosReceipt(ftQueue queue, ftQueueItem existingQueueItem, InMemoryJournalMERepository inMemoryJournalMERepository, InMemoryActionJournalRepository inMemoryActionJournalRepository)
        {
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var tcr = CreateTcr();
            var scu = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
                TcrIntId = tcr.TcrIntId,
                BusinessUnitCode = tcr.BusinessUnitCode,
                IssuerTin = tcr.IssuerTin,
                TcrCode = "TestTCRCode008",
                EnuType = "Regular"
            };
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu).ConfigureAwait(false);
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = scu.ftSignaturCreationUnitMEId
            };
            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueME);
            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();

            if (existingQueueItem != null)
            {
                await inMemoryQueueItemRepository.InsertOrUpdateAsync(existingQueueItem).ConfigureAwait(false);
            }
            var posReceiptCommand = new PosReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), 
                inMemoryConfigurationRepository, inMemoryJournalMERepository, inMemoryQueueItemRepository, inMemoryActionJournalRepository);
            return posReceiptCommand;
        }


        private ReceiptRequest CreateReceiptRequest()
        {
            return new ReceiptRequest()
            {
                ftReceiptCase = 0x44D5_0000_0001_0001,
                ftReceiptCaseData = JsonConvert.SerializeObject(CreateInvoice()),
                cbCustomer = JsonConvert.SerializeObject(CreateBuyer()),
                cbReceiptMoment = DateTime.Now,
                cbReceiptReference = "107",
                cbChargeItems = new ChargeItem[] {
                    new ChargeItem() {
                        Amount = 221,
                        ftChargeItemCase = 0x44D5_0000_0000_0001,
                        ProductBarcode = "Testbarcode1",
                        Unit = "piece",
                        Quantity = 2,
                        UnitPrice = 110.5M,
                        Description = "TestChargeItem1"
                    },
                    new ChargeItem() {
                        Amount = 107,
                        ftChargeItemCase = 0x44D5_0000_0000_0002,
                        ProductBarcode = "Testbarcode2",
                        Unit = "piece",
                        Quantity = 1,
                        UnitPrice = 107,
                        Description = "TestChargeItem2"
                    },
                    new ChargeItem() {
                        Amount = 100,
                        ftChargeItemCase = 0x44D5_0000_0001_0001,
                        ftChargeItemCaseData = JsonConvert.SerializeObject(createVoucherInvoiceItemRequest()),
                        ProductBarcode = "Voucher",
                        Quantity = 1,
                        Description = "Voucher"
                    }
                },
                cbPayItems = new PayItem[]
                {
                    new PayItem()
                    {
                       Amount = 308,
                       ftPayItemCase = 0x44D5_0000_0000_0000,
                    },
                    //Voucher
                    new PayItem()
                    {
                       Amount = 50,
                       ftPayItemCase = 0x44D5_0000_0000_0003,
                       ftPayItemCaseData = @"{'VoucherNumber' : '51234'}",
                    },
                    //Voucher
                    new PayItem()
                    {
                       Amount = 50,
                       ftPayItemCase = 0x44D5_0000_0000_0003,
                       ftPayItemCaseData = @"{'VoucherNumber' : '41234'}",
                    },//Customer
                     new PayItem()
                    {
                       Amount = 10,  
                       ftPayItemCase = 0x44D5_0000_0000_0004,
                       ftPayItemCaseData = @"{'CompCardNumber' : '61234'}",
                    },
                    new PayItem()
                    {
                       Amount = 10,
                       ftPayItemCase = 0x44D5_0000_0000_0004,
                       ftPayItemCaseData =  @"{'CompCardNumber' : '71234'}",
                    }
                }
            };
        }

        private InvoiceItemRequest createVoucherInvoiceItemRequest()
        {
            return new InvoiceItemRequest()
            {
                VoucherExpirationDate = "2023-01-01",
                VoucherSerialNumbers = new string[] { "Voucher", "Voucher2" }
            };
        }

        private Tcr CreateTcr()
        {
            return new Tcr()
            {
                BusinessUnitCode = "aT007FT889",
                IssuerTin = "02657598",
                TcrIntId = Guid.NewGuid().ToString()
            };
        }

        private Invoice CreateInvoice()
        {
            return new Invoice()
            {
                OperatorCode = "ab123ab123",
                PayDeadline = DateTime.Now.AddDays(30),
                CorrectiveInv = new CorrectiveInv()
                {
                    ReferencedIKOF = "TestIICRef",
                    ReferencedMoment = DateTime.Now.AddDays(-2),
                    Type = "Corrective",
                },
                Fees = new Fee[] {
                    new Fee()
                    {
                        Amount = 4,
                        FeeType = "Pack",
                    }
                },
            };
        }

        private Buyer CreateBuyer()
        {
            return new Buyer()
            {
                IdentificationNumber = "72001008",
                BuyerIdentificationType = "TIN",
                Address = "Mustergasse 8",
                Name = "Mr. Mad",
                Town = "City",
                Country = "MNE"
            };
        }
    }
}
