using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Fixtures;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class StammAgenturenFactoryTests : IClassFixture<MasterDataFactoryTestsFixture>
    {
        private readonly MasterDataFactoryTestsFixture _fixture;

        public StammAgenturenFactoryTests(MasterDataFactoryTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput_ForOneAgency()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var masterDataConfig = _fixture.MasterData;

            var sut = new StammAgenturenFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(masterDataConfig, zNumber, closingMoment).ToList();

            result.Should().HaveCount(1);

            var expectedAgency = masterDataConfig.Agencies.First();
            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].AGENTUR_ID.Should().Be(1);
            result[0].AGENTUR_NAME.Should().Be(expectedAgency.Name);
            result[0].AGENTUR_STRASSE.Should().Be(expectedAgency.Street);
            result[0].AGENTUR_PLZ.Should().Be(expectedAgency.Zip);
            result[0].AGENTUR_ORT.Should().Be(expectedAgency.City);
            result[0].AGENTUR_LAND.Should().Be("AUT");
            result[0].AGENTUR_STNR.Should().Be(expectedAgency.TaxId);
            result[0].AGENTUR_USTID.Should().Be(expectedAgency.VatId);
        }
    }
}
