using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest
{
    public class AADEFactoryTests
    {
        public static ChargeItem CreateServiceNormalVATRateItem(string description, decimal amount, decimal quantity)
        {
            var vatRate = 24m;
            return new ChargeItem
            {
                Amount = amount,
                VATRate = vatRate,
                VATAmount = amount / (100M + vatRate) * vatRate,
                ftChargeItemCase = 0x4752_2000_0000_0023,
                Quantity = quantity,
                Description = description
            };
        }

        public static ChargeItem CreateServiceDiscountedVATRateItem(string description, decimal amount, decimal quantity)
        {
            var vatRate = 13m;
            return new ChargeItem
            {
                Amount = amount,
                VATRate = vatRate,
                VATAmount = amount / (100M + vatRate) * vatRate,
                ftChargeItemCase = 0x4752_2000_0000_0023,
                Quantity = quantity,
                Description = description
            };
        }


        public static ChargeItem CreateGoodNormalVATRateItem(string description, decimal amount, decimal quantity)
        {
            var vatRate = 24m;
            return new ChargeItem
            {
                Amount = amount,
                VATRate = vatRate,
                VATAmount = amount / (100M + vatRate) * vatRate,
                ftChargeItemCase = 0x4752_2000_0000_0013,
                Quantity = quantity,
                Description = description
            };
        }

        [Fact]
        public void AADE_Demo_Case1_A1__1_1_SalesInvoice()
        {
            /// Issuing a sale invoice of type Α1_1.1 with 5 lines, 
            /// summarising products and services sold. 
            ///     2 lines will contain products with 24% VAT, 
            ///     1 line for the sale of the provision of a service at 24% VAT and tax withholding of 20% VAT 
            ///     2 lines showing sale of merchandise at 13% VAT.
            ///
            var receiptRequest = new ReceiptRequest
            {
                Currency = Currency.EUR,
                cbReceiptAmount = 100,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems =
                [
                    CreateGoodNormalVATRateItem(description: "Προϊόν 1", amount: 100, quantity: 1),
                    CreateGoodNormalVATRateItem(description: "Προϊόν 1", amount: 100, quantity: 1),
                    CreateServiceNormalVATRateItem(description: "Προϊόν 1", amount: 100, quantity: 1)
                ],
                cbPayItems = [],
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = 0x4752_2000_0000_1001 // invoiceB2c
            };
            var receiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x4752_2000_0000_0000
            };

            var aadFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData
                {
                    VatId = "997671771"
                }
            });
            var invoiceDoc = aadFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            invoiceDoc.invoice.Length.Should().Be(1);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item11);
        }


        [Fact]
        public void AADE_Demo_Case1_A1__1_1_SalesInvoicde()
        {
            /// Issuing a retail sales receipt of type Α2_11.1) with 5 lines, 
            ///     summarising products sold. 
            ///     3 lines will refer to merchandise at 24% VAT and 
            ///     2 lines with merchandise at 13% VAT. 
            ///     The emulated sale must be towards an emulated payment 
            ///     terminal and demonstrate the payload of making a sale while the terminal is offline, 
            ///     according to Α.1155/2023. 
            ///     @Ioannis Pliakis can advise you on what commands you would be needing to send to our payment terminal 
            ///     so you can construct the payload for the emulated Cloud REST API request. 
            ///     You do not need to read A.1155/2023 to figure out the command sequence, Ioannis will help out with that.

            var receiptRequest = new ReceiptRequest
            {
                Currency = Currency.EUR,
                cbReceiptAmount = 1.95m,
                cbReceiptMoment = new DateTime(2024, 10, 22, 0, 0, 0, DateTimeKind.Utc),
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems =
                [
                    CreateGoodNormalVATRateItem(description: "ΠΕΡΙΒ. ΤΕΛΟΣ Π.", amount: 0.05m, quantity: 1),
                    CreateServiceDiscountedVATRateItem(description: "Cappuccino Διπλός", amount: 1.85m, quantity: 1)
                ],
                cbPayItems = [],
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = 0x4752_2000_0000_0001 // invoiceB2c
            };
            var receiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),            
                ftQueueRow = 1,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftReceiptIdentification = "ftB32#",
                ftReceiptMoment = DateTime.UtcNow,
                ftState = 0x4752_2000_0000_0000
            };

            var aadFactory = new AADEFactory(new storage.V0.MasterData.MasterDataConfiguration
            {
                Account = new storage.V0.MasterData.AccountMasterData
                {
                    VatId = "802035962"
                }
            });
            var invoiceDoc = aadFactory.MapToInvoicesDoc(receiptRequest, receiptResponse);

            invoiceDoc.invoice.Length.Should().Be(1);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item111);

            invoiceDoc.invoice[0].uid = aadFactory.GetUid(invoiceDoc.invoice[0]);

            var xml = aadFactory.GenerateInvoicePayload(invoiceDoc);
            File.WriteAllText("invoice.xml", xml);
            
            var dd = GetUid();
            dd.Should().Be("FBA6C7EA5A018D27C94CAFC5A521F6A3259EF0C1");
            invoiceDoc.invoice[0].uid.Should().Be("FBA6C7EA5A018D27C94CAFC5A521F6A3259EF0C1");
        }

        public string GetUid() => BitConverter.ToString(SHA1.HashData(Encoding.UTF8.GetBytes($"802035962-2024-10-22-0-11.1-013-2866"))).Replace("-", "");
    }
}