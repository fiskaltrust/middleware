using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class BonkopfZahlartFactoryTests
    {
        [Fact]
        public void Create_PayitemsWithForeignCurrencyAndNegativQuantities_ValidResult()
        {
            var ftQueueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem
                {
                    cbReceiptMoment = new DateTime(2021, 11, 8, 13, 12, 0),
                    cbReceiptReference = "REF99"
                },
                ReceiptRequest = new ReceiptRequest
                {
                    cbPayItems = new[]
                        {
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000001,
                                Quantity = -1,
                                Amount = 111.11m,
                                Description = "Descipt1"
                            },
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000002,
                                Quantity = -1,
                                ftPayItemCaseData = "{\"ItemCaseName\":\"Other payment\", \"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":\"222.22\", \"BaseCurrencyAmount\":\"122.22\"}"
                            }
                        }
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft3#IT31",
                }
            };
            var cashboxIdentification = "test-cbi";
            var zNumber = 88;
            var closingMoment = new DateTime(2021, 11, 8, 13, 14, 0);
            var sut = new BonkopfZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(ftQueueItem, zNumber, closingMoment).ToList();
            result.Should().HaveCount(2);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].BON_ID.Should().Be("ft3#IT31");
            result[0].ZAHLART_TYP.Should().Be(DSFinVKConstants.PaymentTypes.Cash);
            result[0].ZAHLART_NAME.Should().Be("Descipt1");
            result[0].ZAHLWAEH_CODE.Should().Be(null);
            result[0].BASISWAEH_BETRAG.Should().Be(-111.11M);
            result[0].ZAHLWAEH_BETRAG.Should().Be(null);

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[1].Z_NR.Should().Be(zNumber);
            result[1].BON_ID.Should().Be("ft3#IT31");
            result[1].ZAHLART_TYP.Should().Be(DSFinVKConstants.PaymentTypes.Cash);
            result[1].ZAHLART_NAME.Should().Be("Other payment");
            result[1].ZAHLWAEH_CODE.Should().Be("USD");
            result[1].BASISWAEH_BETRAG.Should().Be(-122.22M);
            result[1].ZAHLWAEH_BETRAG.Should().Be(-222.22M);
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput_ForChargeAndTransformedPayItems()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItems = GetTestData();

            var sut = new BonkopfZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItems, zNumber, closingMoment).ToList();

            result.Should().HaveCount(3);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].BON_ID.Should().Be("ft3#IT31");
            result[0].ZAHLART_TYP.Should().Be(DSFinVKConstants.PaymentTypes.Cash);
            result[0].ZAHLART_NAME.Should().Be("Descipt1");
            result[0].ZAHLWAEH_CODE.Should().Be(null);
            result[0].BASISWAEH_BETRAG.Should().Be(116.00M);
            result[0].ZAHLWAEH_BETRAG.Should().Be(null);

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[1].Z_NR.Should().Be(zNumber);
            result[1].BON_ID.Should().Be("ft3#IT31");
            result[1].ZAHLART_TYP.Should().Be(DSFinVKConstants.PaymentTypes.Cash);
            result[1].ZAHLART_NAME.Should().Be("Other payment");
            result[1].ZAHLWAEH_CODE.Should().Be("USD");
            result[1].BASISWAEH_BETRAG.Should().Be(18.86M);
            result[1].ZAHLWAEH_BETRAG.Should().Be(22.00M);

            result[2].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[2].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[2].Z_NR.Should().Be(zNumber);
            result[2].BON_ID.Should().Be("ft3#IT31");
            result[2].ZAHLART_TYP.Should().Be(DSFinVKConstants.PaymentTypes.CreditCard);
            result[2].ZAHLART_NAME.Should().Be("Descipt2");
            result[2].ZAHLWAEH_CODE.Should().Be(null);
            result[2].BASISWAEH_BETRAG.Should().Be(500.00M);
            result[2].ZAHLWAEH_BETRAG.Should().Be(null);
        }

        private ReceiptEntry GetTestData()
        {
            return new ReceiptEntry()
            {
                QueueItem = new ftQueueItem
                {
                    cbReceiptMoment = new DateTime(2020, 9, 25, 12, 18, 0),
                    cbReceiptReference = "REF1"
                },
                ReceiptRequest = new ReceiptRequest
                {
                    cbPayItems = new[]
                        {
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000001,
                                Amount = 116,
                                Description = "Descipt1"
                            },
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000002,
                                ftPayItemCaseData = "{\"ItemCaseName\":\"Other payment\", \"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":\"22.00\", \"BaseCurrencyAmount\":\"18.86\"}"
                            },
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000005,
                                Amount = 500,
                                Description = "Descipt2"
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
