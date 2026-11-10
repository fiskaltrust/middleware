using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.CashpointClosing;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.CashpointClosing
{
    public class ZWaehrungenFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput_WhenMultipleCurrenciesAreUsed()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var requests = GetTestData()
                .Select(x => JsonConvert.DeserializeObject<ReceiptRequest>(x.request));

            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();

            result.Should().HaveCount(2);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(250.00000M);

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[1].Z_NR.Should().Be(zNumber);
            result[1].ZAHLART_WAEH.Should().Be("USD");
            result[1].ZAHLART_BETRAG_WAEH.Should().Be(22.00000M);
        }

        [Fact]
        public void CreateZWaehrung_NegativQuantityPositivAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var quantity = -1;
            var requests = GetReceiptRequest(amount, null, quantity);           
            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(-amount);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
        }
        [Fact]
        public void CreateZWaehrung_NegativQuantityPositivForeignCurrencyAmount_ExpectNegativForeignCurrencyAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var baseAmount = 1234;
            var foreignCurrencyAmount = 1435.69m;
            var quantity = -1;
            var requests = GetReceiptRequest(baseAmount, foreignCurrencyAmount, quantity);
            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(0);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
            result[1].ZAHLART_BETRAG_WAEH.Should().Be(-foreignCurrencyAmount);
            result[1].ZAHLART_WAEH.Should().Be("USD");
        }
        [Fact]
        public void CreateZWaehrung_PositivQuantityNegativeAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var quantity = 1;
            var requests = GetReceiptRequest(amount, null, quantity);
            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(amount);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
        }
        [Fact]
        public void CreateZWaehrung_PositiveQuantityNegativeForeignCurrencyAmount_ExpectNegativForeignCurrencyAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var baseAmount = -1234;
            var foreignCurrencyAmount = -1435.69m;
            var quantity = 1;
            var requests = GetReceiptRequest(baseAmount, foreignCurrencyAmount, quantity);
            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(0);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
            result[1].ZAHLART_BETRAG_WAEH.Should().Be(foreignCurrencyAmount);
            result[1].ZAHLART_WAEH.Should().Be("USD");
        }
        [Fact]
        public void CreateZWaehrung_NegativeQuantityNegativeAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var quantity = -1;
            var requests = GetReceiptRequest(amount, null, quantity);
            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(amount);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
        }
        [Fact]
        public void CreateZWaehrung_NegativeQuantityNegativeForeignCurrencyAmount_ExpectNegativBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var baseAmount = -1234;
            var foreignCurrencyAmount = -1435.69m;
            var quantity = -1;
            var requests = GetReceiptRequest(baseAmount, foreignCurrencyAmount, quantity);
            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(0);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
            result[1].ZAHLART_BETRAG_WAEH.Should().Be(foreignCurrencyAmount);
            result[1].ZAHLART_WAEH.Should().Be("USD");
        }
        [Fact]
        public void CreateZWaehrung_PositiveQuantityPositiveAmount_ExpectPositiveAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var quantity = 1;
            var requests = GetReceiptRequest(amount, null, quantity);
            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(amount);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
        }
        [Fact]
        public void CreateZWaehrung_PositiveQuantityPositiveForeignCurrencyAmount_ExpectPositiveBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var baseAmount = 1234;
            var foreignCurrencyAmount = 1435.69m;
            var quantity = 1;
            var requests = GetReceiptRequest(baseAmount, foreignCurrencyAmount, quantity);
            var sut = new ZWaehrungenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].ZAHLART_BETRAG_WAEH.Should().Be(0);
            result[0].ZAHLART_WAEH.Should().Be("EUR");
            result[1].ZAHLART_BETRAG_WAEH.Should().Be(foreignCurrencyAmount);
            result[1].ZAHLART_WAEH.Should().Be("USD");
        }

        private ReceiptRequest[] GetReceiptRequest(decimal baseAmount, decimal? foreignCurrencyAmount, decimal quantity)
        {
            return new[]{new ReceiptRequest
            {
                cbReceiptMoment = new DateTime(2021, 10, 19, 6, 40, 12),

                cbPayItems = new[]
                {
                    new PayItem
                    {
                        ftPayItemCase = 0x4445000000000002,
                        Amount =  baseAmount,
                        Quantity = quantity,
                        ftPayItemCaseData = foreignCurrencyAmount.HasValue ? "{\"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":\"" + foreignCurrencyAmount.Value.ToString(CultureInfo.InvariantCulture) + "\", \"BaseCurrencyAmount\":\"" + baseAmount.ToString(CultureInfo.InvariantCulture)+ "\"}" : null
                    }
                }
            }
            };
        }
        private (string cashboxIdentification, int zNumber, DateTime closingMoment) GetIniParams() => ("test-cbi", 77, new DateTime(2021, 10, 20, 6, 40, 12));
        private List<ftQueueItem> GetTestData()
        {
            return new List<ftQueueItem>
            {
                new ftQueueItem
                {
                    request = JsonConvert.SerializeObject(new ReceiptRequest
                    {
                        cbReceiptMoment = new DateTime(2020, 9, 25, 7, 12, 0),
                        cbPayItems = new[]
                        {
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000002,
                                Amount = 0,
                                ftPayItemCaseData = "{\"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":\"22.00\", \"BaseCurrencyAmount\":\"18.86\"}"
                            }
                        }
                    })
                },
                new ftQueueItem
                {
                    request = JsonConvert.SerializeObject(new ReceiptRequest
                    {
                        cbReceiptMoment = new DateTime(2020, 9, 25, 7, 12, 0),
                        cbPayItems = new[]
                        {
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000001,
                                Amount = 150.0M
                            },
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000001,
                                Amount = 100.0M
                            }
                        }
                    })
                }
            };
        }
    }
}
