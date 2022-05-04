using System;
using System.Threading.Tasks;
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

namespace fiskaltrust.Middleware.Localization.QueueME.UnitTest.RequestCommandsTests
{
    public class PosReceiptCommandTests
    {

        [Fact]
        public async Task ExecuteAsync_RegisterInvoice_ValidResultAsync()
        {
            var inMemoryConfigurationRepository = new InMemoryConfigurationRepository();
            var tcr = CreateTCR();
            var queue = new ftQueue()
            {
                ftQueueId = Guid.NewGuid()
            };
            var scu = new ftSignaturCreationUnitME()
            {
                ftSignaturCreationUnitMEId = Guid.NewGuid(),
                TcrIntId = tcr.TCRIntID,
                BusinessUnitCode = tcr.BusinUnitCode,
                IssuerTin = tcr.IssuerTIN,
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
            var receiptRequest = CreateReceiptRequest();
            var posReceiptCommand = new PosReceiptCommand(Mock.Of<ILogger<RequestCommand>>(), new SignatureFactoryME(), inMemoryConfigurationRepository, Mock.Of<IJournalMERepository>());
            var testTcr = "TestTCRCodePos";
            var inMemoryMESSCD = new InMemoryMESSCD(testTcr);

            await posReceiptCommand.ExecuteAsync(inMemoryMESSCD, queue, receiptRequest, new ftQueueItem()).ConfigureAwait(false);

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

        private TCR CreateTCR()
        {
            return new TCR()
            {
                BusinUnitCode = "aT007FT889",
                IssuerTIN = "02657598",
                TCRIntID = Guid.NewGuid().ToString()
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
