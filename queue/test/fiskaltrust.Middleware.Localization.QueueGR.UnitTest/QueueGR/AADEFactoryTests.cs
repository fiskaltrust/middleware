using System.Security.Cryptography;
using System.Text;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueGR.GRSSCD.AADE;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.SAFT.CLI;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueGR.UnitTest
{
    public class AADEFactoryTests
    {
        public static ChargeItem CreateServiceNormalVATRateItem_WithWithHoldingTax(string description, decimal netAmount, decimal quantity)
        {
            var vatRate = 24m;
            var withholdingAmount = decimal.Round(netAmount * (20m / 100m), 2, MidpointRounding.AwayFromZero);
            var vatAmount = netAmount * (vatRate / 100);
            var chargeItem = new ChargeItem
            {
                Amount = netAmount + vatAmount,
                VATRate = vatRate,
                VATAmount = vatAmount,
                ftChargeItemCase = 0x4752_2000_0000_0023,
                Quantity = quantity,
                Description = description,
                ftChargeItemCaseData = new WithHoldingChargeItem
                {
                    WithHoldingPercentage = 20m,
                    WithHoldingAmount = withholdingAmount
                }
            };
            return chargeItem;
        }

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
                ftChargeItemCase = 0x4752_2000_0000_0021,
                Quantity = quantity,
                Description = description
            };
        }

        public static PayItem CreateWithHoldingPayItem(string description, decimal amount)
        {
            var percent = 20m;
            return new PayItem
            {
                Amount = amount * (percent / 100),
                ftPayItemCase = 0x4752_2000_0000_099,
                Quantity = 1,
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

        public static ChargeItem CreateGoodDiscountedVATRateItem(string description, decimal amount, decimal quantity)
        {
            var vatRate = 13m;
            return new ChargeItem
            {
                Amount = amount,
                VATRate = vatRate,
                VATAmount = amount / (100M + vatRate) * vatRate,
                ftChargeItemCase = 0x4752_2000_0000_0011,
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
            var chargeItems = new List<ChargeItem> {
                    CreateGoodNormalVATRateItem(description: "Product 1", amount: 89.20m, quantity: 1),
                    CreateGoodNormalVATRateItem(description: "Product 2", amount: 23.43m, quantity: 1),
                    CreateServiceNormalVATRateItem_WithWithHoldingTax(description: "Service Provision 1", netAmount: 461.93m, quantity: 1),
                    CreateGoodDiscountedVATRateItem(description: "Merchandise Product 1", amount: 12.30m, quantity: 1),
                    CreateGoodDiscountedVATRateItem(description: "Merchandise Product 2", amount: 113.43m, quantity: 1),
                };

            var i = 1;
            foreach (var chargeItem in chargeItems)
            {
                chargeItem.Position = i++;
                // Set fraction
                chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
                chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
            }

            var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = -92.39m,
                    Description = "VAT withholding (-20%)",
                    ftPayItemCase = 0x4752_2000_0000_0099
                },
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount) -  92.39m,
                    Description = "Cash",
                    ftPayItemCase = 0x4752_2000_0000_0001
                }
            };

            i = 1;
            foreach (var payItem in payItems)
            {
                payItem.Position = i++;
                // Set fraction
                payItem.Amount = decimal.Round(payItem.Amount, 2, MidpointRounding.AwayFromZero);
            }

            var receiptRequest = new ReceiptRequest
            {
                Currency = Currency.EUR,
                cbReceiptAmount = 100,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = chargeItems,
                cbPayItems = payItems,
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = 0x4752_2000_0000_1001,
                cbCustomer = new MiddlewareCustomer
                {
                    CustomerVATId = "997671770",

                }
            };
            var receiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftReceiptIdentification = "ft" + DateTime.UtcNow.Ticks.ToString("X"),
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
            var xml = aadFactory.GenerateInvoicePayload(invoiceDoc);
            File.WriteAllText("sales_invoice_1_1.xml", xml);
            invoiceDoc.invoice.Length.Should().Be(1);
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item11);
        }

        [Fact]
        public void AADE_Demo_Case2_A2__11_1_RetailInovoice()
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
            var chargeItems = new List<ChargeItem>
            {
                CreateGoodNormalVATRateItem(description: "Merchandise Product 1", amount: 89.20m, quantity: 1),
                CreateGoodNormalVATRateItem(description: "Merchandise Product 2", amount: 23.43m, quantity: 1),
                CreateGoodNormalVATRateItem(description: "Merchandise Product 3", amount: 4.43m, quantity: 1),
                CreateGoodDiscountedVATRateItem(description: "Merchandise Product Discounted 1", amount: 12.30m, quantity: 1),
                CreateGoodDiscountedVATRateItem(description: "Merchandise Product Discounted 2", amount: 113.43m, quantity: 1)
            };
            var i = 1;
            foreach (var chargeItem in chargeItems)
            {
                chargeItem.Position = i++;
                // Set fraction
                chargeItem.Amount = decimal.Round(chargeItem.Amount, 2, MidpointRounding.AwayFromZero);
                chargeItem.VATAmount = decimal.Round(chargeItem.VATAmount ?? 0.0m, 2, MidpointRounding.AwayFromZero);
            }
            var payItems = new List<PayItem>
            {
                new PayItem
                {
                    Amount = chargeItems.Sum(x => x.Amount),
                    Description = "Card",
                    ftPayItemCase = 0x4752_2000_0000_0000 & (long) PayItemCases.DebitCardPayment,
                    ftPayItemCaseData = new PayItemCaseData
                    {
                        Provider = new PayItemCaseProviderVivaWallet
                        {
                            Action = "Sale",
                            Protocol = "VivaWallet",
                            ProtocolVersion = "1.0",
                            ProtocolRequest = new VivaWalletPayment
                            {
                                amount = (int) chargeItems.Sum(x => x.Amount) * 100,
                                cashRegisterId = "",
                                currencyCode = "EUR",
                                merchantReference = Guid.NewGuid().ToString(),
                                sessionId = "John015",
                                terminalId = "123456",
                                aadeProviderSignatureData = "4680AFE5D58088BF8C55F57A5B5DBB15936B51DE;;20241015153111;4600;9;1;10;16007793",
                                aadeProviderSignature = "MEUCIQCnUrakY9pemgdXIsYvbOahoBBadDa9DPaRS9ZtTTra8gIgIUp9LPaH/E+LRwTGJWeL+MZl5j5PtFcM+chiXTqeed4="
                            },
                            ProtocolResponse = new VivaPaymentSession
                            {
                                aadeTransactionId = "116430909552789552789"
                            }
                        }
                    }
                }
            };
            var receiptRequest = new ReceiptRequest
            {
                Currency = Currency.EUR,
                cbReceiptAmount = 1.95m,
                cbReceiptMoment = DateTime.UtcNow,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = chargeItems,
                cbPayItems = payItems,
                ftCashBoxID = Guid.NewGuid(),
                ftPosSystemId = Guid.NewGuid(),
                ftReceiptCase = 0x4752_2000_0000_0001 // posreceipt
            };
            var receiptResponse = new ReceiptResponse
            {
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftReceiptIdentification = "ft" + DateTime.UtcNow.Ticks.ToString("X"),
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
            invoiceDoc.invoice[0].invoiceHeader.invoiceType.Should().Be(InvoiceType.Item111);

            invoiceDoc.invoice[0].uid = aadFactory.GetUid(invoiceDoc.invoice[0]);

            var xml = aadFactory.GenerateInvoicePayload(invoiceDoc);
            File.WriteAllText("posreceipt.xml", xml);
        }

        [Theory]
        [InlineData("802035962-2024-10-22-0-11.1-013-2866", "FBA6C7EA5A018D27C94CAFC5A521F6A3259EF0C1")]
        [InlineData("800739773-2024-11-01-0-11.1-1253-111002", "B96A69F6054CACCFC9958A0B4757CF2A1A3A76AA")]
        [InlineData("062062972-2024-10-08-0-2.1-0-2970", "40F3AB32183CFBF7F91F5C1A4831E71EA5769792")]
        public void CompareHash(string data, string hash)
        {
            var actualHash = GetUid(data);
            actualHash.Should().Be(hash);
        }

        public string GetUid(string data) => BitConverter.ToString(SHA1.HashData(Encoding.UTF8.GetBytes(data))).Replace("-", "");
    }
}


/**
 * Case 1:
 * **/