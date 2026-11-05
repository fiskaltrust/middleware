using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;
using static fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants.DSFinVKConstants;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class StammUstFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput_ForChargeAndTransformedPayItems()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItems = GetTestData();

            var sut = new StammUstFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, new Mock<ILogger>().Object);
            var result = sut.Create(queueItems, zNumber, closingMoment).ToList();

            result.Should().HaveCount(3);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].UST_SCHLUESSEL.Should().Be(DSFinVKConstants.VatKeys.General);
            result[0].UST_SATZ.Should().Be(19.00M);
            result[0].UST_BESCHR.Should().Be("19,00 % Allgemeiner Steuersatz");

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[1].Z_NR.Should().Be(zNumber);
            result[1].UST_SCHLUESSEL.Should().Be(DSFinVKConstants.VatKeys.Reduced);
            result[1].UST_SATZ.Should().Be(7.00M);
            result[1].UST_BESCHR.Should().Be("7,00 % Ermäßigter Steuersatz");

            result[2].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[2].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[2].Z_NR.Should().Be(zNumber);
            result[2].UST_SCHLUESSEL.Should().Be(DSFinVKConstants.VatKeys.NotTaxable);
            result[2].UST_SATZ.Should().Be(0);
            result[2].UST_BESCHR.Should().Be("0,00 % Nicht Steuerbar");
        }
        [Fact]
        public void TestGetVatDefinition()
        {
            VatDefinition.GetVatDefinition(1000).Should().Be("individuelle Sachverhalte (Altsteuersätze, § 13b UStG, o.ä.)");
            VatDefinition.GetVatDefinition(8).Should().Be("reserviert für Änderungen DSFinV-K");
            VatDefinition.GetVatDefinition(999).Should().Be("reserviert für Änderungen DSFinV-K");
            VatDefinition.GetVatDefinition(7).Should().Be("0,00 % UmsatzsteuerNichtErmittelbar");
            VatDefinition.GetVatDefinition(1).Should().Be("19,00 % Allgemeiner Steuersatz");
            VatDefinition.GetVatDefinition(0).Should().Be("Not defined");
        }

        [Fact]
        public void Create_SpeacialVatRateDefinitiomn_ShowRateDefinition()
        {
            var receipts = new ReceiptEntry() { ReceiptRequest = new ReceiptRequest() { 
                cbChargeItems = new[] { new ChargeItem() {
                    Quantity = 1,
                    Description = "Goldkauf",
                    Amount = 103.00m,
                    VATRate = 3.00m,
                    VATAmount = 3.00m,
                    ftChargeItemCase = 0x4445000000000017, 
                    ftChargeItemCaseData = "{\"VatDefinitionDescription\": \"Differenzbesteuerung1\" }",
                    Moment = DateTime.Now
                    },
                     new ChargeItem() {
                    Quantity = 1,
                    Description = "Goldkauf",
                    Amount = 103.00m,
                    VATRate = 3.00m,
                    VATAmount = 3.00m,
                    ftChargeItemCase = 0x4445000000000017,
                    ftChargeItemCaseData = "{\"VatDefinitionDescription\": \"Differenzbesteuerung2\" }",
                    Moment = DateTime.Now
                    },
                      new ChargeItem() {
                    Quantity = 1,
                    Description = "Goldkauf",
                    Amount = 104.00m,
                    VATRate = 4.00m,
                    VATAmount = 4.00m,
                    ftChargeItemCase = 0x4445000000000017,
                    ftChargeItemCaseData = "{\"VatDefinitionDescription\": \"Differenzbesteuerung3\" }",
                    Moment = DateTime.Now
                    },
                }
            }};
            var sut = new StammUstFactory(new DSFinVKParameters { CashboxIdentification = "Test" }, new Mock<ILogger>().Object);
            var result = sut.Create(new List<ReceiptEntry>() { receipts }, 1, DateTime.Now).ToList();
            result.Should().HaveCount(3);

            var goldkauf = result.FirstOrDefault(x => x.UST_BESCHR == "Differenzbesteuerung1");
            goldkauf.Should().NotBeNull();
            goldkauf.UST_SATZ.Should().Be(3.00m);
            goldkauf.UST_SCHLUESSEL.Should().Be(1300);
            goldkauf = result.FirstOrDefault(x => x.UST_BESCHR == "Differenzbesteuerung2");
            goldkauf.Should().NotBeNull();
            goldkauf.UST_SATZ.Should().Be(3.00m);
            goldkauf.UST_SCHLUESSEL.Should().Be(1300);
            goldkauf = result.FirstOrDefault(x => x.UST_BESCHR == "Differenzbesteuerung3");
            goldkauf.Should().NotBeNull();
            goldkauf.UST_SATZ.Should().Be(4.00m);
            goldkauf.UST_SCHLUESSEL.Should().Be(1400);
        }
        [Theory]
        [InlineData("2026-10-09", 19.00, 7.00)]
        [InlineData("2020-08-15", 20.00, 6.00)] // lowered timeframe 07-12/2020: the sent rate is kept
        public void Create_DeviatingSentVatRate_ShouldNotDuplicateVatKey(string moment, decimal expectedGeneralRate, decimal expectedReducedRate)
        {
            var receiptMoment = DateTime.Parse(moment);
            var receipt = new ReceiptEntry
            {
                QueueItem = new ftQueueItem(),
                ReceiptRequest = new ReceiptRequest
                {
                    cbReceiptMoment = receiptMoment,
                    cbChargeItems = new[]
                    {
                        new ChargeItem { Description = "Rumpsteak", Amount = 57.8m, VATRate = 20m, VATAmount = 9.23m, ftChargeItemCase = 0x4445000000000001 },
                        new ChargeItem { Description = "Weizen", Amount = 5m, VATRate = 19m, ftChargeItemCase = 0x4445000000000001 },
                        new ChargeItem { Description = "Brot", Amount = 3m, VATRate = 6m, ftChargeItemCase = 0x4445000000000002 },
                        new ChargeItem { Description = "Milch", Amount = 2m, VATRate = 7m, ftChargeItemCase = 0x4445000000000002 },
                        new ChargeItem { Description = "Briefmarke", Amount = 1m, VATRate = 19m, ftChargeItemCase = 0x4445000000000006 }
                    }
                }
            };

            var sut = new StammUstFactory(new DSFinVKParameters { CashboxIdentification = "Test" }, new Mock<ILogger>().Object);
            var result = sut.Create(new List<ReceiptEntry> { receipt }, 1, receiptMoment).ToList();

            var expectedRows = receiptMoment.Year == 2020 ? 5 : 3;
            result.Should().HaveCount(expectedRows);
            result.Select(x => (x.UST_SCHLUESSEL, x.UST_SATZ)).Should().Contain((DSFinVKConstants.VatKeys.General, expectedGeneralRate));
            result.Select(x => (x.UST_SCHLUESSEL, x.UST_SATZ)).Should().Contain((DSFinVKConstants.VatKeys.Reduced, expectedReducedRate));
            result.Single(x => x.UST_SCHLUESSEL == DSFinVKConstants.VatKeys.Excempt).UST_SATZ.Should().Be(0m);
        }

        private List<ReceiptEntry> GetTestData()
        {
            return new List<ReceiptEntry>
            {
                new ReceiptEntry(){
                    QueueItem = new ftQueueItem(),
                    ReceiptRequest = new ReceiptRequest
                    {
                        cbChargeItems = new[]
                        {
                            new ChargeItem
                            {
                                ftChargeItemCase = 0x4445000000000001,
                            },
                            new ChargeItem
                            {
                                ftChargeItemCase = 0x4445000000000002
                            }
                        }
                    }
                },
                new ReceiptEntry(){
                    QueueItem = new ftQueueItem(),
                    ReceiptRequest = new ReceiptRequest
                    {
                        cbPayItems = new[]
                        {
                            new PayItem
                            {
                                ftPayItemCase = 0x444500000000000D
                            }
                        }
                    }
                }
            };
        }
    }
}
