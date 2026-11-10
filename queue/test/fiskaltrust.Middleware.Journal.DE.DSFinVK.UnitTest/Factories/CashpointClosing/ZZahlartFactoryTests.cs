using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.CashpointClosing;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.CashpointClosing
{
    public class ZZahlartFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput_WhenMultiplePaymentTypesAreUsed()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var requests = GetTestData()
                .Select(x => JsonConvert.DeserializeObject<ReceiptRequest>(x.request));

            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();

            result.Should().HaveCount(3);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].ZAHLART_TYP.Should().Be(DSFinVKConstants.PaymentTypes.Cash);
            result[0].ZAHLART_NAME.Should().Be(string.Empty);
            result[0].Z_ZAHLART_BETRAG.Should().Be(118.86M);

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[1].Z_NR.Should().Be(zNumber);
            result[1].ZAHLART_TYP.Should().Be(DSFinVKConstants.PaymentTypes.EcCard);
            result[1].ZAHLART_NAME.Should().Be(string.Empty);
            result[1].Z_ZAHLART_BETRAG.Should().Be(100.00000M);

            result[2].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[2].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[2].Z_NR.Should().Be(zNumber);
            result[2].ZAHLART_TYP.Should().Be(DSFinVKConstants.PaymentTypes.CreditCard);
            result[2].ZAHLART_NAME.Should().Be(string.Empty);
            result[2].Z_ZAHLART_BETRAG.Should().Be(70.00000M);
        }
        [Fact]
        public void CreateZZahlartFactory_NegativQuantityPositivAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var quantity = -1;
            var requests = GetReceiptRequest(amount, null, quantity);
            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].Z_ZAHLART_BETRAG.Should().Be(-amount);
        }
        [Fact]
        public void CreateZZahlartFactory_NegativQuantityPositivForeignCurrencyAmount_ExpectNegativBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var quantity = -1;
            var requests = GetReceiptRequest(amount, null, quantity);
            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].Z_ZAHLART_BETRAG.Should().Be(-amount);
        }
        [Fact]
        public void CreateZZahlartFactory_PositivQuantityNegativeAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var quantity = 1;
            var requests = GetReceiptRequest(amount, null, quantity);
            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].Z_ZAHLART_BETRAG.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_PositiveQuantityNegativeForeignCurrencyAmount_ExpectNegativBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var foreignCurrencyAmount = -1435.69m;
            var quantity = 1;
            var requests = GetReceiptRequest(amount, foreignCurrencyAmount, quantity);
            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].Z_ZAHLART_BETRAG.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_NegativeQuantityNegativeAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var quantity = -1;
            var requests = GetReceiptRequest(amount, null, quantity);
            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].Z_ZAHLART_BETRAG.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_NegativeQuantityNegativeForeignCurrencyAmount_ExpectNegativBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var foreignCurrencyAmount = -1435.69m;
            var quantity = -1;
            var requests = GetReceiptRequest(amount, foreignCurrencyAmount, quantity);
            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].Z_ZAHLART_BETRAG.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_PositiveQuantityPositiveAmount_ExpectPositiveAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var quantity = 1;
            var requests = GetReceiptRequest(amount, null, quantity);
            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].Z_ZAHLART_BETRAG.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_PositiveQuantityPositiveForeignCurrencyAmount_ExpectPositiveBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var foreignCurrencyAmount = 1435.69m;
            var quantity = 1;
            var requests = GetReceiptRequest(amount, foreignCurrencyAmount, quantity);
            var sut = new ZZahlartFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(requests, zNumber, closingMoment).ToList();
            result[0].Z_ZAHLART_BETRAG.Should().Be(amount);
        }

        private ReceiptRequest[] GetReceiptRequest(decimal amount, decimal? foreignCurrencyAmount, decimal quantity)
        {
            return new[]{new ReceiptRequest
            {
                cbReceiptMoment = new DateTime(2021, 10, 19, 6, 40, 12),
                cbPayItems = new[]
                {
                    new PayItem
                    {
                        ftPayItemCase = 0x4445000000000002,
                        Amount =  amount,
                        Quantity = quantity,
                        ftPayItemCaseData = foreignCurrencyAmount.HasValue ? "{\"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":\"" + foreignCurrencyAmount.Value.ToString(CultureInfo.InvariantCulture) + "\", \"BaseCurrencyAmount\":\"" + amount.ToString(CultureInfo.InvariantCulture)+ "\"}" : null
                    }
                }
            }};
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
                                ftPayItemCase = 0x4445000000000001,  // Cash in national currency
                                Amount = 100.0M
                            },
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000002,  // Cash in foreign currency
                                Amount = 0,
                                ftPayItemCaseData = "{\"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":\"22.00\", \"BaseCurrencyAmount\":\"18.86\"}"

                            },
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000004, // Debit card
                                Amount = 100.00M
                            },
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000005, // Credit card
                                Amount = 50.00M
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
                                ftPayItemCase = 0x4445000000000005, // Credit card
                                Amount = 20.00M
                            }
                        }
                    })
                }
            };
        }
    }
}
