using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Fixtures;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class StammKassenFactoryTests : IClassFixture<MasterDataFactoryTestsFixture>
    {
        private readonly MasterDataFactoryTestsFixture _fixture;

        public StammKassenFactoryTests(MasterDataFactoryTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput_ForOnePosSystem_Without_UST_ZUORDNUNG()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var masterDataConfig = _fixture.MasterData;
            var queueItems = GetTestData(masterDataConfig.PosSystems.First().PosSystemId);

            var parameters = new DSFinVKParameters { CashboxIdentification = cashboxIdentification };
            var sut = new StammKassenFactory(parameters, new Mock<ILogger>().Object);
            var result = sut.Create(masterDataConfig, queueItems, zNumber, closingMoment).ToList();

            result.Should().HaveCount(1);

            var expectedPosSystem = masterDataConfig.PosSystems.First();
            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].KASSE_BRAND.Should().Be(expectedPosSystem.Brand);
            result[0].KASSE_MODELL.Should().Be(expectedPosSystem.Model);
            result[0].KASSE_SERIENNR.Should().Be(cashboxIdentification);
            result[0].KASSE_SW_BRAND.Should().Be("fiskaltrust.Middleware");
            result[0].KASSE_SW_VERSION.Should().Be(parameters.MiddlewareVersion);
            result[0].KASSE_BASISWAEH_CODE.Should().Be(expectedPosSystem.BaseCurrency);
            result[0].KEINE_UST_ZUORDNUNG.Should().Be("1");

        }

        private List<ReceiptEntry> GetTestData(Guid posSystemId)
        {
            return new List<ReceiptEntry>
            {
                new ReceiptEntry()
                {
                    QueueItem = new ftQueueItem(),
                    ReceiptRequest = new ReceiptRequest
                    {
                        ftPosSystemId = posSystemId.ToString(),
                        cbChargeItems = new[]
                        {
                            new ChargeItem
                            {
                                ftChargeItemCase = 0x4445000000000000
                            }
                        }
                    }
                },
                new ReceiptEntry()
                {
                    QueueItem = new ftQueueItem(),
                    ReceiptRequest = new ReceiptRequest
                    {
                        ftPosSystemId = posSystemId.ToString(),
                        cbChargeItems = new[]
                        {
                            new ChargeItem
                            {
                                ftChargeItemCase = 0x4445000000000001
                            }
                        }
                    }
                }
            };
        }
    }
}
