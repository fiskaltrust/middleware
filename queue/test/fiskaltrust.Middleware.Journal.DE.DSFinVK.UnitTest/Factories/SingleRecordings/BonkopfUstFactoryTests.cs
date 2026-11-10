using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class BonkopfUstFactoryTests
    {
        [Fact]
        public void Create_ChargeItemWithNegativQuanty_ExpectNegativeUMS()
        {
            var ftQueueItem = new ReceiptEntry() { 
                 QueueItem = new ftQueueItem()
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
                            ftChargeItemCase = 0x4445000000000031,
                            Quantity = -1,
                            Amount = 20.0M,
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
            var sut = new BonkopfUstFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(ftQueueItem, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].BON_BRUTTO.Should().Be(-20);
            Math.Round(result[0].BON_NETTO, 2).Should().Be(Math.Round(-16.81m, 2));
            Math.Round(result[0].BON_UST, 2).Should().Be(Math.Round(-3.19m, 2));
        }

        [Fact]
        public void Create_ChargeItemWithNegativAmount_ExpectNegativeUMS()
        {
            var ftQueueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem()
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
                            ftChargeItemCase = 0x4445000000000031,
                            Quantity = 1,
                            Amount = -20.0M,
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
            var sut = new BonkopfUstFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(ftQueueItem, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].BON_BRUTTO.Should().Be(-20);
            Math.Round(result[0].BON_NETTO, 2).Should().Be(Math.Round(-16.81m, 2));
            Math.Round(result[0].BON_UST, 2).Should().Be(Math.Round(-3.19m, 2));
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
                            ftChargeItemCase = 0x4445000000000031,
                            Quantity = -1,
                            Amount = -20.0M,
                            VATRate = 19
                        }
                    },
                    cbPayItems = new []
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
            var sut = new BonkopfUstFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(ftQueueItem, zNumber, closingMoment).ToList();
            result.Should().HaveCount(1);
            result[0].BON_BRUTTO.Should().Be(-20);
            Math.Round(result[0].BON_NETTO, 2).Should().Be(Math.Round(-16.81m, 2));
            Math.Round(result[0].BON_UST, 2).Should().Be(Math.Round(-3.19m, 2));
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput_ForChargeAndTransformedPayItems()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItems = GetTestData();

            var sut = new BonkopfUstFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(queueItems, zNumber, closingMoment).ToList();

            result.Should().HaveCount(2);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].BON_ID.Should().Be("ft3#IT31");
            result[0].UST_SCHLUESSEL.Should().Be(DSFinVKConstants.VatKeys.General);
            result[0].BON_BRUTTO.Should().Be(116);
            result[0].BON_NETTO.Should().Be(Math.Round(100m, 2));
            result[0].BON_UST.Should().Be(Math.Round(16m, 2));

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[1].Z_NR.Should().Be(zNumber);
            result[1].BON_ID.Should().Be("ft3#IT31");
            result[1].UST_SCHLUESSEL.Should().Be(DSFinVKConstants.VatKeys.NotTaxable);
            result[1].BON_BRUTTO.Should().Be(50);
            result[1].BON_NETTO.Should().Be(Math.Round(50m, 2));
            result[1].BON_UST.Should().Be(Math.Round(0m, 2));
        }

        private ReceiptEntry GetTestData()
        {
            return new ReceiptEntry()
            {
                QueueItem = new ftQueueItem
                {
                    cbReceiptReference = "REF1"
                },
                ReceiptRequest = new ReceiptRequest
                {
                    cbReceiptMoment = new DateTime(2020, 9, 25, 12, 18, 0),
                    cbChargeItems = new[]
                        {
                            new ChargeItem
                            {
                                ftChargeItemCase = 0x4445000000000001,
                                Amount = 116,
                                VATRate = 16
                            },
                        },
                    cbPayItems = new[]
                        {
                            new PayItem
                            {
                                ftPayItemCase = 0x444500000000000D,
                                Amount = -50
                            }
                        }
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft3#IT31",
                }
            };
        }
    }
}
