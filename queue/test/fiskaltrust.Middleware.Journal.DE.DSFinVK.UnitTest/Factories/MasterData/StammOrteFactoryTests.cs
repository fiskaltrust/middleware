using System;
using System.Collections.Generic;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.MasterData;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Fixtures;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class StammOrteFactoryTests : IClassFixture<MasterDataFactoryTestsFixture>
    {
        private readonly MasterDataFactoryTestsFixture _fixture;

        public StammOrteFactoryTests(MasterDataFactoryTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var masterDataConfig = _fixture.MasterData;

            var sut = new StammOrteFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(masterDataConfig, zNumber, closingMoment);

            result.Z_KASSE_ID.Should().Be(cashboxIdentification);
            result.Z_ERSTELLUNG.Should().Be(closingMoment);
            result.Z_NR.Should().Be(zNumber);
            result.LOC_NAME.Should().Be(masterDataConfig.Outlet.OutletName);
            result.LOC_STRASSE.Should().Be(masterDataConfig.Outlet.Street);
            result.LOC_PLZ.Should().Be(masterDataConfig.Outlet.Zip);
            result.LOC_ORT.Should().Be(masterDataConfig.Outlet.City);
            result.LOC_LAND.Should().Be("DEU");
            result.LOC_USTID.Should().Be(masterDataConfig.Outlet.VatId);
        }
    }
}
