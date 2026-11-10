using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Fixtures;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class StammAbschlussFactoryTests : IClassFixture<MasterDataFactoryTestsFixture>
    {
        private readonly MasterDataFactoryTestsFixture _fixture;

        public StammAbschlussFactoryTests(MasterDataFactoryTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput_WithDifferentPaymentTypes()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItems = GetTestData();
            var masterDataConfig = _fixture.MasterData;

            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(masterDataConfig, queueItems, zNumber, closingMoment);

            result.Z_KASSE_ID.Should().Be(cashboxIdentification);
            result.Z_ERSTELLUNG.Should().Be(closingMoment);
            result.Z_NR.Should().Be(zNumber);
            result.Z_BUCHUNGSTAG.Should().Be(string.Empty);
            result.TAXONOMIE_VERSION.Should().Be(TaskConstants.Versions.TAXONOMIE);
            result.Z_START_ID.Should().Be("ft1#IT1");
            result.Z_ENDE_ID.Should().Be("ft2#IT2");
            result.NAME.Should().Be(masterDataConfig.Account.AccountName);
            result.STRASSE.Should().Be(masterDataConfig.Account.Street);
            result.PLZ.Should().Be(masterDataConfig.Account.Zip);
            result.ORT.Should().Be(masterDataConfig.Account.City);
            result.LAND.Should().Be("DEU");
            result.STNR.Should().Be(masterDataConfig.Account.TaxId);
            result.USTID.Should().Be(masterDataConfig.Account.VatId);
            result.Z_SE_ZAHLUNGEN.Should().Be(268.86M);
            result.Z_SE_BARZAHLUNGEN.Should().Be(118.86M);
        }

        [Fact]
        public void CreateZZahlartFactory_NegativQuantityPositivAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var quantity = -1;
            var ftQueueItems = GetftQueueItem(amount, null, quantity);
            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(null, ftQueueItems, zNumber, closingMoment);
            result.Z_SE_BARZAHLUNGEN.Should().Be(-amount);
            result.Z_SE_ZAHLUNGEN.Should().Be(-amount);
        }
        [Fact]
        public void CreateZZahlartFactory_NegativQuantityPositivForeignCurrencyAmount_ExpectNegativBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var quantity = -1;
            var foreignCurrencyAmount = 1435.69m;
            var ftQueueItems = GetftQueueItem(amount, foreignCurrencyAmount, quantity);
            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(null, ftQueueItems, zNumber, closingMoment);
            result.Z_SE_BARZAHLUNGEN.Should().Be(-amount);
            result.Z_SE_ZAHLUNGEN.Should().Be(-amount);
        }
        [Fact]
        public void CreateZZahlartFactory_PositivQuantityNegativeAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var quantity = 1;
            var ftQueueItems = GetftQueueItem(amount, null, quantity);
            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(null, ftQueueItems, zNumber, closingMoment);
            result.Z_SE_BARZAHLUNGEN.Should().Be(amount);
            result.Z_SE_ZAHLUNGEN.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_PositiveQuantityNegativeForeignCurrencyAmount_ExpectNegativBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var foreignCurrencyAmount = -1435.69m;
            var quantity = 1;
            var ftQueueItems = GetftQueueItem(amount, foreignCurrencyAmount, quantity);
            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(null, ftQueueItems, zNumber, closingMoment);
            result.Z_SE_BARZAHLUNGEN.Should().Be(amount);
            result.Z_SE_ZAHLUNGEN.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_NegativeQuantityNegativeAmount_ExpectNegativAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var quantity = -1;
            var ftQueueItems = GetftQueueItem(amount, null, quantity);
            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(null, ftQueueItems, zNumber, closingMoment);
            result.Z_SE_BARZAHLUNGEN.Should().Be(amount);
            result.Z_SE_ZAHLUNGEN.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_NegativeQuantityNegativeForeignCurrencyAmount_ExpectNegativBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = -1234;
            var foreignCurrencyAmount = -1435.69m;
            var quantity = -1;
            var ftQueueItems = GetftQueueItem(amount, foreignCurrencyAmount, quantity);
            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(null, ftQueueItems, zNumber, closingMoment);
            result.Z_SE_BARZAHLUNGEN.Should().Be(amount);
            result.Z_SE_ZAHLUNGEN.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_PositiveQuantityPositiveAmount_ExpectPositiveAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var quantity = 1;
            var ftQueueItems = GetftQueueItem(amount, null, quantity);
            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(null, ftQueueItems, zNumber, closingMoment);
            result.Z_SE_BARZAHLUNGEN.Should().Be(amount);
            result.Z_SE_ZAHLUNGEN.Should().Be(amount);
        }
        [Fact]
        public void CreateZZahlartFactory_PositiveQuantityPositiveForeignCurrencyAmount_ExpectBaseAmount()
        {
            (var cashboxIdentification, var zNumber, var closingMoment) = GetIniParams();
            var amount = 1234;
            var foreignCurrencyAmount = 1435.69m;
            var quantity = 1;
            var receiptEntries = GetftQueueItem(amount, foreignCurrencyAmount, quantity);
            var sut = new StammAbschlussFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(null, receiptEntries, zNumber, closingMoment);
            result.Z_SE_BARZAHLUNGEN.Should().Be(amount);
            result.Z_SE_ZAHLUNGEN.Should().Be(amount);
        }

        private ReceiptEntry[] GetftQueueItem(decimal amount, decimal? foreignCurrencyAmount, decimal quantity)
        { 
            return new ReceiptEntry[]
            {
                new ReceiptEntry(){
                    ReceiptResponse = new ReceiptResponse{ftReceiptIdentification = "ft1#IT1"},
                    ReceiptRequest = new ReceiptRequest
                    {
                        cbReceiptMoment = new DateTime(2021, 10, 19, 6, 40, 12),
                        cbPayItems = new[]
                        {
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000002,
                                Amount = foreignCurrencyAmount.HasValue ? 0 : amount,
                                Quantity = quantity,
                                ftPayItemCaseData = foreignCurrencyAmount.HasValue ? "{\"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":\"" + foreignCurrencyAmount.Value.ToString(CultureInfo.InvariantCulture) + "\", \"BaseCurrencyAmount\":\"" + amount.ToString(CultureInfo.InvariantCulture)+ "\"}" : null
                            }
                        }
                    },
                    QueueItem = new ftQueueItem()
                }
            };
        }
        private (string cashboxIdentification, int zNumber, DateTime closingMoment) GetIniParams() => ("test-cbi", 77, new DateTime(2021, 10, 20, 6, 40, 12));
        private List<ReceiptEntry> GetTestData()
        {
            return new List<ReceiptEntry>
            {
                new ReceiptEntry(){
                        ReceiptResponse = new ReceiptResponse{ftReceiptIdentification = "ft1#IT1"},
                        ReceiptRequest = new ReceiptRequest
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
                                    ftPayItemCase = 0x4445000000000004, // Debit card
                                    Amount = 100.00M
                                },
                                new PayItem
                                {
                                    ftPayItemCase = 0x4445000000000005, // Credit card
                                    Amount = 50.00M
                                }
                            }
                        },
                    QueueItem = new ftQueueItem()
                },
                new ReceiptEntry(){
                    ReceiptResponse = new ReceiptResponse{ftReceiptIdentification = "ft2#IT2"},
                    ReceiptRequest = new ReceiptRequest
                    {
                        cbReceiptMoment = new DateTime(2020, 9, 25, 7, 12, 0),
                        cbPayItems = new[]
                        {
                            new PayItem
                            {
                                ftPayItemCase = 0x4445000000000002,  // Cash in foreign currency
                                Amount = 0,
                                ftPayItemCaseData = "{\"CurrencyCode\":\"USD\", \"ForeignCurrencyAmount\":\"22.00\", \"BaseCurrencyAmount\":\"18.86\"}"

                            }
                        }
                    },
                QueueItem = new ftQueueItem()
            } };
        }
    }
}
