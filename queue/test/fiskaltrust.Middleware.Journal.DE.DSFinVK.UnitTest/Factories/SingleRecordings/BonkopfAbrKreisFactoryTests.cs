using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class BonkopfAbrKreisFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItems = GetTestData();

            var sut = new BonkopfAbrKreisFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItems, zNumber, closingMoment).ToList();

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].BON_ID.Should().Be("ft1#IT1");
            result[0].ABRECHNUNGSKREIS.Should().Be("REC1");
        }

        private ReceiptEntry GetTestData()
        {
            return new ReceiptEntry() { 
                QueueItem = new ftQueueItem
                {
                    cbReceiptReference = "Cancelation1"
                },
                ReceiptRequest = new ReceiptRequest
                {
                    cbPreviousReceiptReference = "REC1"
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft1#IT1"
                }
            };
        }
    }
}