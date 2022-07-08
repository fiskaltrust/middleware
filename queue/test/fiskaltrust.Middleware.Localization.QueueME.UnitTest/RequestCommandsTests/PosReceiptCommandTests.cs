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
            var queue = new ftQueue
            {
                ftQueueId = Guid.NewGuid()
            };
            var queueME = new ftQueueME
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var now = DateTime.UtcNow;
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, Guid.NewGuid(), now).ConfigureAwait(false);
            var queueItem = new ftQueueItem() { ftQueueItemId = Guid.NewGuid(), ftWorkMoment = now };
            var (posReceiptCommand, tcr, scu) = await InitializePosReceipt(queueItem, new InMemoryJournalMERepository(), inMemoryActionJournalRepository, queueME).ConfigureAwait(false);
            var receiptRequest = CreateReceiptRequest(now);
            var iic = "iic";
            var inMemoryMESSCD = new InMemoryMESSCD(scu.TcrCode, iic, "iicSignature");
            var requestCommandResponse = await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem, queueME).ConfigureAwait(false);
            requestCommandResponse.ReceiptResponse.Should().NotBeNull();
            requestCommandResponse.ReceiptResponse.ftSignatures.Should().NotBeNull();
            requestCommandResponse.ReceiptResponse.ftSignatures.Should().HaveCount(2);
            requestCommandResponse.ReceiptResponse.ftSignatures[0].Data.Should().Be(iic);
            requestCommandResponse.ReceiptResponse.ftSignatures[1].Data.Should().Match($"https://efitest.tax.gov.me/ic/#/verify?iic={iic}&tin={tcr.IssuerTin}&crtd={now.ToString(@"yyyy-MM-dd\THH:mm:ss\Z")}&ord=1&bu={tcr.BusinessUnitCode}&cr={scu.TcrCode}&sw=&prc=428");
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
                ftJournalMEId = Guid.NewGuid(),
                ftQueueItemId = existingQueueItem.ftQueueItemId,
                ftQueueId = existingQueueItem.ftQueueId,
                ftOrdinalNumber = 8,
                JournalType = (long) JournalTypes.JournalME
            };
            await inMemoryJournalMERepository.InsertAsync(journal).ConfigureAwait(false);
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, existingQueueItem.ftQueueItemId, DateTime.Now).ConfigureAwait(false);
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var (posReceiptCommand, tcr, scu) = await InitializePosReceipt(existingQueueItem, inMemoryJournalMERepository, inMemoryActionJournalRepository, queueME).ConfigureAwait(false);
            var receiptRequest = CreateReceiptRequest(DateTime.Now);
            var inMemoryMESSCD = new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature");
            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };

            await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem, queueME).ConfigureAwait(false);
            var journalMEs = await inMemoryJournalMERepository.GetAsync().ConfigureAwait(false);
            var journalME = journalMEs.Where(x => x.ftQueueItemId.Equals(queueItem.ftQueueItemId));
            Assert.Single(journalME);
            journalME.FirstOrDefault().ftOrdinalNumber.Should().Be(9);
        }

        [Fact]
        public async Task ExecuteAsync_CashDepositOutstanding_Exception()
        {
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var queue = new ftQueue
            {
                ftQueueId = Guid.NewGuid()
            };
            var queueMe = new ftQueueME
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var tcr = CreateTcr();
            var scu = new ftSignaturCreationUnitME
            {
                ftSignaturCreationUnitMEId = queueMe.ftSignaturCreationUnitMEId.Value,
                TcrIntId = tcr.TcrIntId,
                BusinessUnitCode = tcr.BusinessUnitCode,
                IssuerTin = tcr.IssuerTin,
                TcrCode = "TestTCRCode008",
                EnuType = "Regular"
            };
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu).ConfigureAwait(false);
            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueMe);

            var existingQueueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.UtcNow.AddDays(-1)
            };
            var inMemoryJournalMeRepository = new InMemoryJournalMERepository();
            var journal = new ftJournalME()
            {
                ftQueueItemId = existingQueueItem.ftQueueItemId,
                ftQueueId = existingQueueItem.ftQueueId,
                ftOrdinalNumber = 8,
                JournalType = (long) JournalTypes.JournalME
            };
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, existingQueueItem.ftQueueItemId, DateTime.UtcNow.AddDays(-1)).ConfigureAwait(false);
            await inMemoryJournalMeRepository.InsertAsync(journal).ConfigureAwait(false);
            var receiptRequest = CreateReceiptRequest(DateTime.Now);
            var posReceiptCommand = new PosReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, inMemoryJournalMeRepository, new InMemoryQueueItemRepository(), inMemoryActionJournalRepository, new QueueMEConfiguration { Sandbox = true });
            var sutMethod = CallInitialOperationReceiptCommand(posReceiptCommand, queue, queueMe, receiptRequest);
            await sutMethod.Should().ThrowAsync<CashDepositOutstandingException>().ConfigureAwait(false);
        }

        private static async Task<InMemoryActionJournalRepository> IniActionJournalRepo(ftQueue queue, Guid ftQueueItemId, DateTime datetime)
        {
            var inMemoryActionJournalRepository = new InMemoryActionJournalRepository();
            var actionJournal = new ftActionJournal()
            {
                ftActionJournalId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftQueueItemId = ftQueueItemId,
                Type = "4959870564618469383",
                Moment = datetime
            };
            await inMemoryActionJournalRepository.InsertAsync(actionJournal).ConfigureAwait(false);
            return inMemoryActionJournalRepository;
        }

        private Func<Task> CallInitialOperationReceiptCommand(PosReceiptCommand posReceiptCommand, ftQueue queue, ftQueueME queueMe, ReceiptRequest receiptRequest)
        {
            return async () => { var receiptResponse = await posReceiptCommand.ExecuteAsync(new InMemoryMESSCD("testTcr", "iic", "iicSignature"), queue, receiptRequest, new ftQueueItem() { ftWorkMoment = DateTime.Now }, queueMe); };
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
                ftOrdinalNumber = 8,
                JournalType = (long) JournalTypes.JournalME
            };
            await inMemoryJournalMERepository.InsertAsync(journal).ConfigureAwait(false);
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, existingQueueItem.ftQueueItemId, DateTime.Now).ConfigureAwait(false);
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var (posReceiptCommand, tcr, scu) = await InitializePosReceipt(existingQueueItem, inMemoryJournalMERepository, inMemoryActionJournalRepository, queueME).ConfigureAwait(false);
            var receiptRequest = CreateReceiptRequest(DateTime.Now);
            var inMemoryMESSCD = new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature");
            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };

            await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem, queueME).ConfigureAwait(false);
            var journalMEs = await inMemoryJournalMERepository.GetAsync().ConfigureAwait(false);
            var journalME = journalMEs.Where(x => x.ftQueueItemId.Equals(queueItem.ftQueueItemId));
            Assert.Single(journalME);
            journalME.FirstOrDefault().ftOrdinalNumber.Should().Be(1);
        }

        private static async Task AddCashDeposit(Guid queueId, InMemoryJournalMERepository inMemoryJournalMeRepository)
        {
            var journal = new ftJournalME()
            {
                ftJournalMEId = Guid.NewGuid(),
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queueId,
                ftOrdinalNumber = 8,
                JournalType = 4959870564618469383
            };
            await inMemoryJournalMeRepository.InsertAsync(journal).ConfigureAwait(false);
        }

        public async Task<(PosReceiptCommand, Tcr, ftSignaturCreationUnitME)> InitializePosReceipt(ftQueueItem existingQueueItem, InMemoryJournalMERepository inMemoryJournalMERepository, InMemoryActionJournalRepository inMemoryActionJournalRepository, ftQueueME queueMe)
        {
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var tcr = CreateTcr();
            var scu = new ftSignaturCreationUnitME
            {
                ftSignaturCreationUnitMEId = queueMe.ftSignaturCreationUnitMEId.Value,
                TcrIntId = tcr.TcrIntId,
                BusinessUnitCode = tcr.BusinessUnitCode,
                IssuerTin = tcr.IssuerTin,
                TcrCode = "TestTCRCode008",
                EnuType = "Regular"
            };
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu).ConfigureAwait(false);
            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueMe);
            await AddCashDeposit(queueMe.ftQueueMEId, inMemoryJournalMERepository).ConfigureAwait(false);

            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();
            if (existingQueueItem != null)
            {
                existingQueueItem.ftQueueMoment = DateTime.UtcNow.AddMinutes(-1);
                await inMemoryQueueItemRepository.InsertOrUpdateAsync(existingQueueItem).ConfigureAwait(false);
            }
            var posReceiptCommand = new PosReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, inMemoryJournalMERepository, inMemoryQueueItemRepository, inMemoryActionJournalRepository, new QueueMEConfiguration { Sandbox = true });
            return (posReceiptCommand, tcr, scu);
        }

        public static ReceiptRequest CreateReceiptRequest(DateTime now)
        {
            return new ReceiptRequest
            {
                ftReceiptCase = 0x44D5_0000_0001_0001,
                ftReceiptCaseData = JsonConvert.SerializeObject(CreateInvoice()),
                cbCustomer = JsonConvert.SerializeObject(CreateBuyer()),
                cbReceiptMoment = now,
                cbReceiptReference = "107",
                cbChargeItems = new [] {
                    new ChargeItem {
                        Amount = 221,
                        ftChargeItemCase = 0x44D5_0000_0000_0001,
                        ProductBarcode = "Testbarcode1",
                        Unit = "piece",
                        Quantity = 2,
                        UnitPrice = 110.5M,
                        Description = "TestChargeItem1"
                    },
                    new ChargeItem {
                        Amount = 107,
                        ftChargeItemCase = 0x44D5_0000_0000_0002,
                        ProductBarcode = "Testbarcode2",
                        Unit = "piece",
                        Quantity = 1,
                        UnitPrice = 107,
                        Description = "TestChargeItem2"
                    },
                    new ChargeItem {
                        Amount = 100,
                        ftChargeItemCase = 0x44D5_0000_0001_0001,
                        ftChargeItemCaseData = JsonConvert.SerializeObject(CreateVoucherInvoiceItemRequest()),
                        ProductBarcode = "Voucher",
                        Quantity = 1,
                        Description = "Voucher"
                    }
                },
                cbPayItems = new []
                {
                    new PayItem
                    {
                       Amount = 308,
                       ftPayItemCase = 0x44D5_0000_0000_0000,
                    },
                    //Voucher
                    new PayItem
                    {
                       Amount = 50,
                       ftPayItemCase = 0x44D5_0000_0000_0003,
                       ftPayItemCaseData = @"{'VoucherNumber' : '51234'}",
                    },
                    //Voucher
                    new PayItem
                    {
                       Amount = 50,
                       ftPayItemCase = 0x44D5_0000_0000_0003,
                       ftPayItemCaseData = @"{'VoucherNumber' : '41234'}",
                    },//Customer
                     new PayItem
                    {
                       Amount = 10,  
                       ftPayItemCase = 0x44D5_0000_0000_0004,
                       ftPayItemCaseData = @"{'CompCardNumber' : '61234'}",
                    },
                    new PayItem
                    {
                       Amount = 10,
                       ftPayItemCase = 0x44D5_0000_0000_0004,
                       ftPayItemCaseData =  @"{'CompCardNumber' : '71234'}",
                    }
                }
            };
        }

        private static InvoiceItemRequest CreateVoucherInvoiceItemRequest()
        {
            return new InvoiceItemRequest
            {
                VoucherExpirationDate = "2023-01-01",
                VoucherSerialNumbers = new[] { "Voucher", "Voucher2" }
            };
        }

        private static Tcr CreateTcr()
        {
            return new Tcr
            {
                BusinessUnitCode = "aT007FT889",
                IssuerTin = "02657598",
                TcrIntId = Guid.NewGuid().ToString()
            };
        }

        private static Invoice CreateInvoice()
        {
            return new Invoice
            {
                OperatorCode = "ab123ab123",
                PayDeadline = DateTime.Now.AddDays(30),
                Fees = new[] {
                    new Fee
                    {
                        Amount = 4,
                        FeeType = "Pack",
                    }
                },
            };
        }

        private static Buyer CreateBuyer()
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
