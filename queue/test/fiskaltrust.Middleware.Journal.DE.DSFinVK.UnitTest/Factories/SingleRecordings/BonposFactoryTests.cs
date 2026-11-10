using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using Moq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class BonposFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItem = GetTestData();

            var sut = new BonposFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification }, Mock.Of<Microsoft.Extensions.Logging.ILogger>());
            var result = sut.Create(queueItem, zNumber, closingMoment).ToList();

            result.Should().HaveCount(2);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].BON_ID.Should().Be("ft1#IT1");
            result[0].POS_ZEILE.Should().Be("0");
            result[0].GUTSCHEIN_NR.Should().Be(null);
            result[0].ARTIKELTEXT.Should().Be("Bier 0,5l");
            result[0].POS_TERMINAL_ID.Should().Be(null);
            result[0].GV_TYP.Should().Be("Umsatz");
            result[0].GV_NAME.Should().Be(string.Empty);
            result[0].P_STORNO.Should().Be(false);
            result[0].INHAUS.Should().Be(true);
            result[0].AGENTUR_ID.Should().Be(0);
            result[0].ART_NR.Should().Be("1234");
            result[0].GTIN.Should().Be("G1234");
            result[0].WARENGR_ID.Should().Be("3984299583");
            result[0].WARENGR.Should().Be("Drinks");
            result[0].MENGE.Should().Be(1);
            result[0].FAKTOR.Should().Be(1);
            result[0].EINHEIT.Should().Be("Liter");
            result[0].STK_BR.Should().Be(3.80M);

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[1].Z_NR.Should().Be(zNumber);
            result[1].BON_ID.Should().Be("ft1#IT1");
            result[1].POS_ZEILE.Should().Be("1");
            result[1].GUTSCHEIN_NR.Should().Be(null);
            result[1].ARTIKELTEXT.Should().Be("Gutschein");
            result[1].POS_TERMINAL_ID.Should().Be(null);
            result[1].GV_TYP.Should().Be("MehrzweckgutscheinEinloesung");
            result[1].GV_NAME.Should().Be(string.Empty);
            result[1].P_STORNO.Should().Be(false);
            result[1].INHAUS.Should().Be(true);
            result[1].AGENTUR_ID.Should().Be(0);
            result[1].ART_NR.Should().Be("321");
            result[1].GTIN.Should().Be(null);
            result[1].WARENGR_ID.Should().Be("1784898846");
            result[1].WARENGR.Should().Be("Vouchers");
            result[1].MENGE.Should().Be(-1);
            result[1].FAKTOR.Should().Be(null);
            result[1].EINHEIT.Should().Be(null);
            result[1].STK_BR.Should().Be(19.99M);
        }

        private ReceiptEntry GetTestData()
        {
            return new ReceiptEntry
            {
                ReceiptRequest = new ReceiptRequest
                {
                    cbTerminalID = "T1",
                    cbChargeItems = new[]
                    {
                        new ChargeItem
                        {
                            Description = "Bier 0,5l",
                            ftChargeItemCase = 0x4445000000000001,
                            ProductNumber = "1234",
                            ProductBarcode = "G1234",
                            ProductGroup = "Drinks",
                            Unit = "Liter",
                            Amount = 3.80M,
                            Quantity = 1,
                            UnitPrice = 3.80M,
                            UnitQuantity = 1
                        }
                    },
                    cbPayItems = new[]
                    {
                        new PayItem
                        {
                            ftPayItemCase = 0x444500000000000D,
                            Description = "Gutschein",
                            MoneyNumber = "321",
                            MoneyGroup = "Vouchers",
                            Quantity = 1,
                            Amount = 19.99M
                        },
                        new PayItem
                        {
                            ftPayItemCase = 0x4445000000000001,
                            Description = "Cash",
                            Quantity = 1,
                            Amount = -16.19M
                        }
                    }
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft1#IT1",
                }
            };
        }

        [Fact]
        public void TestGetDateTimeFromSignature()
        {
            var expectedMoment = new DateTime(1981, 9, 8, 11, 52, 00, DateTimeKind.Utc);
            var response = new ReceiptResponse()
            {
                ftSignatures = new SignaturItem[]
                {
                    new SignaturItem(){
                        ftSignatureType = (long) SignatureTypesDE.LogTime,
                        Data = expectedMoment.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                }
            };
            var actualMoment = ReceiptResponseExtensions.GetDateTimeFromSignature(response, SignatureTypesDE.LogTime).Value;
            expectedMoment.Should().BeSameDateAs(actualMoment);
        }

        [Fact]
        public void TestReviceQuanityWhenNegative()
        {
            BonposFactory.ReviseQuanityWhenNegative(2, 9).Should().Be(2);
            BonposFactory.ReviseQuanityWhenNegative(3, -4).Should().Be(-3);
            BonposFactory.ReviseQuanityWhenNegative(-1, 3).Should().Be(-1);
            BonposFactory.ReviseQuanityWhenNegative(-1, -12).Should().Be(-1);
        }
    }
}
