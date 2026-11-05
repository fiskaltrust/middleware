using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.CashpointClosing;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.CashpointClosing
{
    public class ZGVTypFactoryTests
    {
        [Fact]
        public void Create_ChargeItemWithNegativQuanty_ExpectNegativeUMS()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbReceiptMoment = new DateTime(2021, 11, 10, 7, 12, 0),
                cbChargeItems = new[]
                {
                        new ChargeItem
                        {
                            ftChargeItemCase = 0x4445000000000031,
                            Quantity = -1,
                            Amount = 20.0M,
                            VATRate = 19
                        }
                    }
            };
            var cashboxIdentification = "test-cbi";
            var zNumber = 8;
            var closingMoment = new DateTime(2021, 11, 10, 10, 04, 12);
            var sut = new ZGVTypFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(new List<ReceiptRequest> { receiptRequest }, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].Z_UMS_BRUTTO.Should().Be(-20);
            Math.Round(result[0].Z_UMS_NETTO, 2).Should().Be(-16.81m);
            Math.Round(result[0].Z_UST, 2).Should().Be(-3.19m);
        }

        [Fact]
        public void Create_ChargeItemWithNegativAmount_ExpectNegativeUMS()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbReceiptMoment = new DateTime(2021, 11, 10, 7, 12, 0),
                cbChargeItems = new[]
                {
                        new ChargeItem
                        {
                            ftChargeItemCase = 0x4445000000000031,
                            Quantity = 1,
                            Amount = -20.0M,
                            VATRate = 19
                        }
                    }
            };
            var cashboxIdentification = "test-cbi";
            var zNumber = 8;
            var closingMoment = new DateTime(2021, 11, 10, 10, 04, 12);
            var sut = new ZGVTypFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(new List<ReceiptRequest> { receiptRequest }, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].Z_UMS_BRUTTO.Should().Be(-20);
            Math.Round(result[0].Z_UMS_NETTO, 2).Should().Be(-16.81m);
            Math.Round(result[0].Z_UST, 2).Should().Be(-3.19m);
        }
        [Fact]
        public void Create_ChargeItemWithNegativAmountAndQuantity_ExpectNegativeUMS()
        {
            var receiptRequest = new ReceiptRequest
            {
                cbReceiptMoment = new DateTime(2021, 11, 10, 7, 12, 0),
                cbChargeItems = new[]
                {
                        new ChargeItem
                        {
                            ftChargeItemCase = 0x4445000000000031,
                            Quantity = -1,
                            Amount = -20.0M,
                            VATRate = 19
                        }
                    }
            };
            var cashboxIdentification = "test-cbi";
            var zNumber = 8;
            var closingMoment = new DateTime(2021, 11, 10, 10, 04, 12);
            var sut = new ZGVTypFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(new List<ReceiptRequest> { receiptRequest }, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].Z_UMS_BRUTTO.Should().Be(-20);
            Math.Round(result[0].Z_UMS_NETTO, 2).Should().Be(-16.81m);
            Math.Round(result[0].Z_UST, 2).Should().Be(-3.19m);
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput_FromChargeItems()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var requests = GetChargeItemTestData();

            var sut = new ZGVTypFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(requests, zNumber, closingMoment).ToList();

            result.Should().HaveCount(1);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].GV_TYP.Should().Be("Umsatz");
            result[0].GV_NAME.Should().Be(string.Empty);
            result[0].AGENTUR_ID.Should().Be(0);
            result[0].UST_SCHLUESSEL.Should().Be(DSFinVKConstants.VatKeys.General);
            result[0].Z_UMS_BRUTTO.Should().Be(116);
            result[0].Z_UMS_NETTO.Should().Be(100);
            result[0].Z_UST.Should().BeApproximately(16.00000M, 0.00001M);
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput_FromTransformedPayItems()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var requests = GetPayItemTestData();

            var sut = new ZGVTypFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(requests, zNumber, closingMoment).ToList();

            result.Should().HaveCount(1);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].GV_TYP.Should().Be("MehrzweckgutscheinKauf");
            result[0].GV_NAME.Should().Be(string.Empty);
            result[0].AGENTUR_ID.Should().Be(0);
            result[0].UST_SCHLUESSEL.Should().Be(DSFinVKConstants.VatKeys.NotTaxable);
            result[0].Z_UMS_BRUTTO.Should().Be(150);
            result[0].Z_UMS_NETTO.Should().Be(150);
            result[0].Z_UST.Should().Be(0);
        }

        private List<ReceiptRequest> GetChargeItemTestData()
        {
            return new List<ReceiptRequest>
            {
                new ReceiptRequest
                {
                    cbReceiptMoment = new DateTime(2020, 9, 25, 7, 12, 0),
                    cbChargeItems = new[]
                    {
                        new ChargeItem
                        {
                            ftChargeItemCase = 0x4445000000000001,
                            Amount = 100.0M,
                            VATRate = 16
                        },
                        new ChargeItem
                        {
                            ftChargeItemCase = 0x4445000000000001,
                            Amount = 16.0M,
                            VATRate = 16
                        }
                    }
                }
            };
        }

        private List<ReceiptRequest> GetPayItemTestData()
        {
            return new List<ReceiptRequest>
            {
                new ReceiptRequest
                {
                    cbReceiptMoment = new DateTime(2020, 9, 25, 7, 12, 0),
                    cbPayItems = new[]
                    {
                        new PayItem
                        {
                            ftPayItemCase = 0x444500000000000D,
                            Amount = -150.0M,
                            ftPayItemCaseData = "{\"VoucherNr\":\"UAUA91829182HH\"}"
                        },
                        new PayItem
                        {
                            ftPayItemCase = 0x4445000000000001,
                            Amount = 150.0M,
                        }
                    }
                }
            };
        }

        [Theory]
        [InlineData(0x4445000000000000, false)]
        [InlineData(0x4445000000040000, true)]
        public void Create_CashInOrOutWithVat_ShouldUseSignForBusinessCaseType_AndVatKeyFromCase(long receiptCaseFlags, bool isVoid)
        {
            var sign = isVoid ? -1 : 1;
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = receiptCaseFlags | 0x0011,
                cbReceiptMoment = new DateTime(2026, 10, 6, 13, 41, 41),
                cbChargeItems = new[]
                {
                    new ChargeItem { ftChargeItemCase = 0x44450000000000B1, Quantity = 1, Amount = sign * -50.0M, VATRate = 19 },
                    new ChargeItem { ftChargeItemCase = 0x44450000000000B2, Quantity = 1, Amount = sign * -50.0M, VATRate = 7 },
                    new ChargeItem { ftChargeItemCase = 0x44450000000000B2, Quantity = 1, Amount = sign * 30.0M, VATRate = 7 },
                    new ChargeItem { ftChargeItemCase = 0x44450000000000B5, Quantity = 1, Amount = sign * -20.0M, VATRate = 0 },
                    // negative quantity with positive amount counts as negative, like in the middleware
                    new ChargeItem { ftChargeItemCase = 0x44450000000000B1, Quantity = sign * -1, Amount = 10.0M, VATRate = 19 }
                }
            };
            var sut = new ZGVTypFactory(new DSFinVKParameters { CashboxIdentification = "test-cbi" }, new Mock<ILogger>().Object);
            var result = sut.Create(new List<ReceiptRequest> { receiptRequest }, 8, new DateTime(2026, 10, 6, 13, 48, 14)).ToList();

            result.Should().HaveCount(4);
            result.Single(x => x.GV_TYP == DSFinVKConstants.BusinessCaseTypes.CashTransferFromCashbook && x.UST_SCHLUESSEL == DSFinVKConstants.VatKeys.General).Z_UMS_BRUTTO.Should().Be(sign * -60.0M);
            result.Single(x => x.GV_TYP == DSFinVKConstants.BusinessCaseTypes.CashTransferFromCashbook && x.UST_SCHLUESSEL == DSFinVKConstants.VatKeys.Reduced).Z_UMS_BRUTTO.Should().Be(sign * -50.0M);
            result.Single(x => x.GV_TYP == DSFinVKConstants.BusinessCaseTypes.CashTransferToCashBook && x.UST_SCHLUESSEL == DSFinVKConstants.VatKeys.Reduced).Z_UMS_BRUTTO.Should().Be(sign * 30.0M);
            result.Single(x => x.GV_TYP == DSFinVKConstants.BusinessCaseTypes.CashTransferFromCashbook && x.UST_SCHLUESSEL == DSFinVKConstants.VatKeys.NotTaxable).Z_UST.Should().Be(0);
        }
    }
}
