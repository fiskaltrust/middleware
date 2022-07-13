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
            var businessUnitCode = "abc1234";
            var issuerTin = "12345";
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
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, Guid.NewGuid(), now);
            var queueItem = new ftQueueItem() { ftQueueItemId = Guid.NewGuid(), ftWorkMoment = now };
            var (posReceiptCommand, scu) = await CreateSut(queueItem, new InMemoryJournalMERepository(), inMemoryActionJournalRepository, queueME,
                queue.ftQueueId.ToString(), businessUnitCode, issuerTin);
            var receiptRequest = CreateReceiptRequest(now);
            var iic = "iic";
            var inMemoryMESSCD = new InMemoryMESSCD(scu.TcrCode, iic, "iicSignature");
            var requestCommandResponse = await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem, queueME);
            requestCommandResponse.ReceiptResponse.Should().NotBeNull();
            requestCommandResponse.ReceiptResponse.ftSignatures.Should().NotBeNull();
            requestCommandResponse.ReceiptResponse.ftSignatures.Should().HaveCount(2);
            requestCommandResponse.ReceiptResponse.ftSignatures[0].Data.Should().Be(iic);
            requestCommandResponse.ReceiptResponse.ftSignatures[1].Data.Should().Match($"https://efitest.tax.gov.me/ic/#/verify?iic={iic}&tin={issuerTin}&crtd={now.ToString(@"yyyy-MM-dd\THH:mm:ss\Z")}&ord=1&bu={businessUnitCode}&cr={scu.TcrCode}&sw=&prc=428");
        }

        [Fact]
        public async Task ExecuteAsync_RegisterSecondInvoice_IncrementOrdNr()
        {
            var businessUnitCode = "abc1234";
            var issuerTin = "12345";
            var queue = new ftQueue
            {
                ftQueueId = Guid.NewGuid()
            };
            var existingQueueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };
            var inMemoryJournalMeRepository = new InMemoryJournalMERepository();
            var journal = new ftJournalME
            {
                ftJournalMEId = Guid.NewGuid(),
                ftQueueItemId = existingQueueItem.ftQueueItemId,
                ftQueueId = existingQueueItem.ftQueueId,
                ftOrdinalNumber = 8,
                JournalType = (long) JournalTypes.JournalME,
                Number = 8
            };
            await inMemoryJournalMeRepository.InsertAsync(journal);
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, existingQueueItem.ftQueueItemId, DateTime.Now);
            var queueME = new ftQueueME
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var (posReceiptCommand, scu) = await CreateSut(existingQueueItem, inMemoryJournalMeRepository, inMemoryActionJournalRepository, queueME,
                queue.ftQueueId.ToString(), businessUnitCode, issuerTin);
            var receiptRequest = CreateReceiptRequest(DateTime.Now);
            var inMemoryMESSCD = new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature");
            var queueItem = new ftQueueItem
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };

            await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem, queueME);
            var journalMEs = await inMemoryJournalMeRepository.GetAsync();
            var journalME = journalMEs.Where(x => x.ftQueueItemId.Equals(queueItem.ftQueueItemId)).FirstOrDefault();
            journalME.Should().NotBeNull();
            journalME.ftOrdinalNumber.Should().Be(9);
        }

        [Fact]
        public async Task ExecuteAsync_CashDepositOutstanding_Exception()
        {
            var businessUnitCode = "abc1234";
            var issuerTin = "12345";

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
            var scu = new ftSignaturCreationUnitME
            {
                ftSignaturCreationUnitMEId = queueMe.ftSignaturCreationUnitMEId.Value,
                TcrIntId = queue.ftQueueId.ToString(),
                BusinessUnitCode = businessUnitCode,
                IssuerTin = issuerTin,
                TcrCode = "TestTCRCode008",
                EnuType = "Regular"
            };
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu);
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
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, existingQueueItem.ftQueueItemId, DateTime.UtcNow.AddDays(-1));
            await inMemoryJournalMeRepository.InsertAsync(journal);
            var receiptRequest = CreateReceiptRequest(DateTime.Now);
            var queueMEConfiguration = new QueueMEConfiguration { Sandbox = true };
            var posReceiptCommand = new PosReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, inMemoryJournalMeRepository, 
                new InMemoryQueueItemRepository(), inMemoryActionJournalRepository, queueMEConfiguration, new Factories.SignatureItemFactory(queueMEConfiguration));
            var sutMethod = CallInitialOperationReceiptCommand(posReceiptCommand, queue, queueMe, receiptRequest);
            await sutMethod.Should().ThrowAsync<CashDepositOutstandingException>();
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
            await inMemoryActionJournalRepository.InsertAsync(actionJournal);
            return inMemoryActionJournalRepository;
        }

        private Func<Task> CallInitialOperationReceiptCommand(PosReceiptCommand posReceiptCommand, ftQueue queue, ftQueueME queueMe, ReceiptRequest receiptRequest)
        {
            return async () => { var receiptResponse = await posReceiptCommand.ExecuteAsync(new InMemoryMESSCD("testTcr", "iic", "iicSignature"), queue, receiptRequest, new ftQueueItem() { ftWorkMoment = DateTime.Now }, queueMe); };
        }


        [Fact]
        public async Task ExecuteAsync_RegisterInvoiceNextYear_ResetOrdNr()
        {
            var businessUnitCode = "abc1234";
            var issuerTin = "12345";

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
            await inMemoryJournalMERepository.InsertAsync(journal);
            var inMemoryActionJournalRepository = await IniActionJournalRepo(queue, existingQueueItem.ftQueueItemId, DateTime.Now);
            var queueME = new ftQueueME()
            {
                ftQueueMEId = queue.ftQueueId,
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
            };
            var (posReceiptCommand, scu) = await CreateSut(existingQueueItem, inMemoryJournalMERepository, inMemoryActionJournalRepository, queueME, 
                queue.ftQueueId.ToString(), businessUnitCode, issuerTin);
            var receiptRequest = CreateReceiptRequest(DateTime.Now);
            var inMemoryMESSCD = new InMemoryMESSCD("TestTCRCodePos", "iic", "iicSignature");
            var queueItem = new ftQueueItem()
            {
                ftQueueItemId = Guid.NewGuid(),
                ftQueueId = queue.ftQueueId,
                ftWorkMoment = DateTime.Now
            };

            await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, queueItem, queueME);
            var journalMEs = await inMemoryJournalMERepository.GetAsync();
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
            await inMemoryJournalMeRepository.InsertAsync(journal);
        }

        public async Task<(PosReceiptCommand, ftSignaturCreationUnitME)> CreateSut(ftQueueItem existingQueueItem, InMemoryJournalMERepository inMemoryJournalMERepository, InMemoryActionJournalRepository inMemoryActionJournalRepository, ftQueueME queueMe,
            string tcrIntId, string businessUnitCode, string issuerTin)
        {
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var scu = new ftSignaturCreationUnitME
            {
                ftSignaturCreationUnitMEId = queueMe.ftSignaturCreationUnitMEId.Value,
                TcrIntId = tcrIntId,
                BusinessUnitCode = businessUnitCode,
                IssuerTin = issuerTin,
                TcrCode = "TestTCRCode008",
                EnuType = "Regular"
            };
            await inMemoryConfigurationRepository.InsertOrUpdateSignaturCreationUnitMEAsync(scu);
            await inMemoryConfigurationRepository.InsertOrUpdateQueueMEAsync(queueMe);
            await AddCashDeposit(queueMe.ftQueueMEId, inMemoryJournalMERepository);

            var inMemoryQueueItemRepository = new InMemoryQueueItemRepository();
            if (existingQueueItem != null)
            {
                existingQueueItem.ftQueueMoment = DateTime.UtcNow.AddMinutes(-1);
                await inMemoryQueueItemRepository.InsertOrUpdateAsync(existingQueueItem);
                await inMemoryJournalMERepository.InsertAsync(new ftJournalME
                {
                    IIC = "TestIIC",
                    ftQueueItemId = existingQueueItem.ftQueueItemId,
                    Number = 3,
                    cbReference = "103",
                    FIC = "TestFic",
                    JournalType = (long) JournalTypes.JournalME,
                    TimeStamp = DateTime.UtcNow.Ticks,
                    ftOrdinalNumber = 0,
                    ftQueueId = queueMe.ftQueueMEId,
                    ftJournalMEId = Guid.NewGuid(),
                    ftInvoiceNumber = "TestInvoiceNr"
                });
            }
            var queueMeConfiguration = new QueueMEConfiguration { Sandbox = true };
            var posReceiptCommand = new PosReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), inMemoryConfigurationRepository, inMemoryJournalMERepository, 
                inMemoryQueueItemRepository, inMemoryActionJournalRepository, queueMeConfiguration, new Factories.SignatureItemFactory(queueMeConfiguration));
            return (posReceiptCommand, scu);
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

        private static Invoice CreateInvoice()
        {
            return new Invoice
            {
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
