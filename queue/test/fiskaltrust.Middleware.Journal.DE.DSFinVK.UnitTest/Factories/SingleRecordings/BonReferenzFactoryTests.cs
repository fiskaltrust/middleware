using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class BonReferenzFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput()
        {
            var cashboxIdentification = "test-cbi";
            var targetZNumber = 77;
            var souceZNumber = 76;
            var targetClosingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var sourceClosingMoment = new DateTime(2020, 8, 18, 7, 17, 12);
            var receiptsreferences = GetTestData(targetZNumber, souceZNumber, targetClosingMoment, sourceClosingMoment);

            var sut = new BonReferenzFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification});
            var result = sut.Create(receiptsreferences).ToList();

            result.Should().HaveCount(2);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(targetClosingMoment);
            result[0].Z_NR.Should().Be(targetZNumber);
            result[0].BON_ID.Should().Be("ft1#IT1");
            result[0].POS_ZEILE.Should().Be(null);
            result[0].REF_TYP.Should().Be("Transaktion");
            result[0].REF_DATUM.Should().Be(new DateTime(2020, 8, 21, 12, 12, 32));
            result[0].REF_Z_NR.Should().Be(12);
            result[0].REF_BON_ID.Should().Be("x123#IT123");
            result[0].REF_NAME.Should().Be(null);
            result[0].REF_Z_KASSE_ID.Should().Be("other-test-cbi");

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(targetClosingMoment);
            result[1].Z_NR.Should().Be(targetZNumber);
            result[1].BON_ID.Should().Be("ft1#IT2");
            result[1].POS_ZEILE.Should().Be(null);
            result[1].REF_TYP.Should().Be("Transaktion");
            result[1].REF_DATUM.Should().Be(sourceClosingMoment);
            result[1].REF_Z_NR.Should().Be(souceZNumber);
            result[1].REF_BON_ID.Should().Be("ft1#IT1");
        }

        private List<ReceiptReferencesGroupedData> GetTestData(int targetZNumber, int souceZNumber, DateTime targetClosingMoment, DateTime sourceClosingMoment)
        {
            return new List<ReceiptReferencesGroupedData>
            {
                new ReceiptReferencesGroupedData
                {
                    TargetQueueItemId = Guid.NewGuid(),
                    TargetQueueRow = 10,
                    TargetReceiptCaseData = "{\"RefType\": \"Transaktion\", \"RefMoment\": \"2020-08-21T12:12:32\", \"RefCashBoxIdentification\": \"other-test-cbi\", \"RefClosingNr\": 12, \"RefReceiptId\": \"x123#IT123\"}",
                    TargetReceiptIdentification = "ft1#IT1",
                    TargetReceiptReference = "R1",
                    TargetZNumber = targetZNumber,
                    TargetZMoment = targetClosingMoment,
                    SourceZNumber = 12,
                    SourceReceiptIdentification = "x123#IT123",
                    SourceZMoment = DateTime.Parse("2020-08-21T12:12:32")
                },
                new ReceiptReferencesGroupedData
                {
                    TargetQueueItemId = Guid.NewGuid(),
                    TargetQueueRow = 9,
                    TargetReceiptIdentification = "ft1#IT2",
                    TargetReceiptReference = "R1",
                    TargetZNumber = targetZNumber,
                    TargetZMoment = targetClosingMoment,
                    SourceQueueItemId = Guid.NewGuid(),
                    SourceZNumber = souceZNumber,
                    SourceReceiptIdentification = "ft1#IT1",
                    SourceZMoment = sourceClosingMoment
                }
            };
        }
    }
}
