using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class BonposZusatzinfoFactoryTests
    {
        [Fact]
        public void Create_ChargeItemWithNegativQuanty_ExpectNegativeUMS()
        {
            var ftQueueItem = new ReceiptEntry
            {
                QueueItem =new ftQueueItem
                {
                    cbReceiptReference = "REF1",
                },
                ReceiptRequest = new ReceiptRequest
                {
                    cbReceiptMoment = new DateTime(2021, 11, 10, 7, 12, 0),
                    cbChargeItems = new[]
                    {
                        new ChargeItem
                        {
                            ftChargeItemCaseData = "{\"SubItems\":[{\"Description\":\"voucher\", \"Quantity\": -1, \"UnitQuantity\": 1,  \"Amount\": 20, \"ftSubChargeItemCase\": 0x4445000000000031 }]}",
                            VATRate = 19
                        }
                    }
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft3#IT31",
                }
            };
            var cashboxIdentification = "test-cbi";
            var zNumber = 8;
            var closingMoment = new DateTime(2021, 11, 10, 10, 04, 12);
            var sut = new BonposZusatzinfoFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(ftQueueItem, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].ZI_BASISPREIS_BRUTTO.Should().Be(-20);
            Math.Round(result[0].ZI_BASISPREIS_NETTO, 2).Should().Be(-16.81m);
            Math.Round(result[0].ZI_BASISPREIS_UST, 2).Should().Be(-3.19m);
        }

        [Fact]
        public void Create_ChargeItemWithNegativAmount_ExpectNegativeUMS()
        {
            var ftQueueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem
                {
                    cbReceiptReference = "REF1"
                },
                ReceiptRequest = new ReceiptRequest
                {
                    cbReceiptMoment = new DateTime(2021, 11, 10, 7, 12, 0),
                    cbChargeItems = new[]
                {
                        new ChargeItem
                        {
                            ftChargeItemCaseData = "{\"SubItems\":[{\"Description\":\"voucher\", \"Quantity\": 1, \"UnitQuantity\": 1,  \"Amount\": -20, \"ftSubChargeItemCase\": 0x4445000000000031 }]}",
                            VATRate = 19
                        }
                    }
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft3#IT31",
                }
            };
            var cashboxIdentification = "test-cbi";
            var zNumber = 8;
            var closingMoment = new DateTime(2021, 11, 10, 10, 04, 12);
            var sut = new BonposZusatzinfoFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(ftQueueItem, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].ZI_BASISPREIS_BRUTTO.Should().Be(-20);
            Math.Round(result[0].ZI_BASISPREIS_NETTO, 2).Should().Be(-16.81m);
            Math.Round(result[0].ZI_BASISPREIS_UST, 2).Should().Be(-3.19m);
        }
        [Fact]
        public void Create_ChargeItemWithNegativAmountAndQuantity_ExpectNegativeUMS()
        {
            var ftQueueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem
                {
                    cbReceiptReference = "REF1"
                },
                ReceiptRequest = new ReceiptRequest
                {
                    cbReceiptMoment = new DateTime(2021, 11, 10, 7, 12, 0),
                    cbChargeItems = new[]
                    {
                        new ChargeItem
                        {
                            ftChargeItemCaseData = "{\"SubItems\":[{\"Description\":\"voucher\", \"Quantity\": -1, \"UnitQuantity\": 1,  \"Amount\": -20, \"ftSubChargeItemCase\": 0x4445000000000031 }]}",
                            VATRate = 19
                        }
                    },
                    cbPayItems = new[]
                    {
                        new PayItem
                        {
                            Amount = -20,
                            Quantity = -1
                        }
                    }
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft3#IT31",
                }
            };
            var cashboxIdentification = "test-cbi";
            var zNumber = 8;
            var closingMoment = new DateTime(2021, 11, 10, 10, 04, 12);
            var sut = new BonposZusatzinfoFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(ftQueueItem, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].ZI_BASISPREIS_BRUTTO.Should().Be(-20);
            Math.Round(result[0].ZI_BASISPREIS_NETTO, 2).Should().Be(-16.81m);
            Math.Round(result[0].ZI_BASISPREIS_UST, 2).Should().Be(-3.19m);
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItem = GetTestData();

            var sut = new BonposZusatzinfoFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(queueItem, zNumber, closingMoment).ToList();

            result.Should().HaveCount(1);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].BON_ID.Should().Be("ft1#IT1");
            result[0].POS_ZEILE.Should().Be("0");
            result[0].ZI_ART_NR.Should().Be("123");
            result[0].ZI_GTIN.Should().Be("G123");
            result[0].ZI_NAME.Should().Be("Bier 0,5l");
            result[0].ZI_WARENGR.Should().Be("Drinks");
            result[0].ZI_WARENGR_ID.Should().Be("1");
            result[0].ZI_MENGE.Should().Be(1);
            result[0].ZI_FAKTOR.Should().Be(1);
            result[0].ZI_EINHEIT.Should().Be("Liter");
            result[0].ZI_UST_SCHLUESSEL.Should().Be(2);
            result[0].ZI_BASISPREIS_BRUTTO.Should().Be(5.25M);
            result[0].ZI_BASISPREIS_NETTO.Should().Be(5);
            result[0].ZI_BASISPREIS_UST.Should().Be(0.25M);
        }

        private ReceiptEntry GetTestData()
        {
            return new ReceiptEntry()
            {
                QueueItem = new ftQueueItem(),
                ReceiptRequest = new ReceiptRequest
                {
                    cbTerminalID = "T1",
                    cbChargeItems = new[]
                            {
                                new ChargeItem
                                {
                                    ftChargeItemCaseData = "{\"SubItems\":[{\"Description\":\"Bier 0,5l\", \"ProductNumber\":\"123\", \"GTIN\":\"G123\", \"ProductGroup\":{\"ProductGroupId\": \"1\", \"ProductGroupName\": \"Drinks\"}, \"Quantity\": 1, \"UnitQuantity\": 1, \"Unit\": \"Liter\", \"Amount\": 5.25, \"ftSubChargeItemCase\": 0x4445000000000002 }]}",
                                    Moment = new DateTime(2020, 9, 26, 12, 13, 0),
                                    VATRate = 5.00M
                                }
                            }
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft1#IT1",
                }
            };
        }
    }
}
