using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueDE.MasterData;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest
{
    public class MasterDataServiceTests
    {
        [Fact]
        public async Task GetCurrentConfigurationAsync_ShouldReturnDataFromRepos()
        {
            var fixture = new Fixture();
            var accountMasterData = fixture.Create<AccountMasterData>();
            var outletMasterData = fixture.Create<OutletMasterData>();
            var agencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var posSystemMasterData = fixture.CreateMany<PosSystemMasterData>();

            var accountMasterDataRepoMock = new Mock<IMasterDataRepository<AccountMasterData>>(MockBehavior.Strict);
            accountMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(new[] { accountMasterData });

            var outletMasterDataRepoMock = new Mock<IMasterDataRepository<OutletMasterData>>(MockBehavior.Strict);
            outletMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(new[] { outletMasterData });

            var agencyMasterDataRepoMock = new Mock<IMasterDataRepository<AgencyMasterData>>(MockBehavior.Strict);
            agencyMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(agencyMasterData);

            var posSystemMasterDataRepoMock = new Mock<IMasterDataRepository<PosSystemMasterData>>(MockBehavior.Strict);
            posSystemMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(posSystemMasterData);

            var sut = new MasterDataService(new MiddlewareConfiguration(), accountMasterDataRepoMock.Object, outletMasterDataRepoMock.Object,
                posSystemMasterDataRepoMock.Object, agencyMasterDataRepoMock.Object);

            var actualConfiguration = await sut.GetCurrentDataAsync();
            actualConfiguration.Account.Should().Be(accountMasterData);
            actualConfiguration.Outlet.Should().Be(outletMasterData);
            actualConfiguration.Agencies.Should().BeEquivalentTo(agencyMasterData);
            actualConfiguration.PosSystems.Should().BeEquivalentTo(posSystemMasterData);
        }

        [Fact]
        public async Task PersistConfigurationAsync_ShouldUpdateDataInRepositories()
        {
            var fixture = new Fixture();
            var accountMasterData = fixture.Create<AccountMasterData>();
            var outletMasterData = fixture.Create<OutletMasterData>();
            var agencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var posSystemMasterData = fixture.CreateMany<PosSystemMasterData>();

            var mwConfig = GetMiddlewareConfig(accountMasterData, outletMasterData, agencyMasterData, posSystemMasterData);

            var accountMasterDataRepoMock = new Mock<IMasterDataRepository<AccountMasterData>>();
            var outletMasterDataRepoMock = new Mock<IMasterDataRepository<OutletMasterData>>();
            var agencyMasterDataRepoMock = new Mock<IMasterDataRepository<AgencyMasterData>>();
            var posSystemMasterDataRepoMock = new Mock<IMasterDataRepository<PosSystemMasterData>>();

            var sut = new MasterDataService(mwConfig, accountMasterDataRepoMock.Object, outletMasterDataRepoMock.Object,
                posSystemMasterDataRepoMock.Object, agencyMasterDataRepoMock.Object);

            await sut.PersistConfigurationAsync();

            accountMasterDataRepoMock.Verify(x => x.CreateAsync(It.Is<AccountMasterData>(m => m.VatId == accountMasterData.VatId)), Times.Once());
            outletMasterDataRepoMock.Verify(x => x.CreateAsync(It.Is<OutletMasterData>(m => m.VatId == outletMasterData.VatId)), Times.Once());
            agencyMasterDataRepoMock.Verify(x => x.CreateAsync(It.IsAny<AgencyMasterData>()), Times.Exactly(agencyMasterData.Count()));
            posSystemMasterDataRepoMock.Verify(x => x.CreateAsync(It.IsAny<PosSystemMasterData>()), Times.Exactly(posSystemMasterData.Count()));
        }

        [Fact]
        public async Task HasDataChanged_ShouldReturnTrue_IfConfigIsDifferentThanDatabase()
        {
            var fixture = new Fixture();
            var confAccountMasterData = fixture.Create<AccountMasterData>();
            var confOutletMasterData = fixture.Create<OutletMasterData>();
            var confAgencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var confPosSystemMasterData = fixture.CreateMany<PosSystemMasterData>();

            var accountMasterData = fixture.Create<AccountMasterData>();
            var outletMasterData = fixture.Create<OutletMasterData>();
            var agencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var posSystemMasterData = fixture.CreateMany<PosSystemMasterData>();

            var mwConfig = GetMiddlewareConfig(confAccountMasterData, confOutletMasterData, confAgencyMasterData, confPosSystemMasterData);
            var accountMasterDataRepoMock = new Mock<IMasterDataRepository<AccountMasterData>>(MockBehavior.Strict);
            accountMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(new[] { accountMasterData });

            var outletMasterDataRepoMock = new Mock<IMasterDataRepository<OutletMasterData>>(MockBehavior.Strict);
            outletMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(new[] { outletMasterData });

            var agencyMasterDataRepoMock = new Mock<IMasterDataRepository<AgencyMasterData>>(MockBehavior.Strict);
            agencyMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(agencyMasterData);

            var posSystemMasterDataRepoMock = new Mock<IMasterDataRepository<PosSystemMasterData>>(MockBehavior.Strict);
            posSystemMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(posSystemMasterData);

            var sut = new MasterDataService(mwConfig, accountMasterDataRepoMock.Object, outletMasterDataRepoMock.Object,
                posSystemMasterDataRepoMock.Object, agencyMasterDataRepoMock.Object);

            (await sut.HasDataChangedAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task HasDataChanged_ShouldReturnFalse_IfConfigIsEqualToDatabase()
        {
            var fixture = new Fixture();
            var accountMasterData = fixture.Create<AccountMasterData>();
            var outletMasterData = fixture.Create<OutletMasterData>();
            var agencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var posSystemMasterData = fixture.CreateMany<PosSystemMasterData>();

            var mwConfig = GetMiddlewareConfig(accountMasterData, outletMasterData, agencyMasterData, posSystemMasterData);
            var accountMasterDataRepoMock = new Mock<IMasterDataRepository<AccountMasterData>>(MockBehavior.Strict);
            accountMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(new[] { accountMasterData });

            var outletMasterDataRepoMock = new Mock<IMasterDataRepository<OutletMasterData>>(MockBehavior.Strict);
            outletMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(new[] { outletMasterData });

            var agencyMasterDataRepoMock = new Mock<IMasterDataRepository<AgencyMasterData>>(MockBehavior.Strict);
            agencyMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(agencyMasterData);

            var posSystemMasterDataRepoMock = new Mock<IMasterDataRepository<PosSystemMasterData>>(MockBehavior.Strict);
            posSystemMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(posSystemMasterData);

            var sut = new MasterDataService(mwConfig, accountMasterDataRepoMock.Object, outletMasterDataRepoMock.Object,
                posSystemMasterDataRepoMock.Object, agencyMasterDataRepoMock.Object);

            (await sut.HasDataChangedAsync()).Should().BeFalse();
        }

        private static MiddlewareConfiguration GetMiddlewareConfig(AccountMasterData accountMasterData, OutletMasterData outletMasterData, IEnumerable<AgencyMasterData> agencyMasterData, IEnumerable<PosSystemMasterData> posSystemMasterData)
        {
            var masterData = new MasterDataConfiguration
            {
                Account = accountMasterData,
                Outlet = outletMasterData,
                Agencies = agencyMasterData,
                PosSystems = posSystemMasterData
            };
            var mwConfig = new MiddlewareConfiguration
            {
                Configuration = new Dictionary<string, object>
                {
                    {"init_masterData", JsonConvert.SerializeObject(masterData) }
                },
                ProcessingVersion = "test"
            };
            return mwConfig;
        }

        [Fact]
        public void GetFromConfig_DBDataAvailable_ExpectConfigData()
        {
            var fixture = new Fixture();
            var confAccountMasterData = fixture.Create<AccountMasterData>();
            var confOutletMasterData = fixture.Create<OutletMasterData>();
            var confAgencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var confPosSystemMasterData = fixture.CreateMany<PosSystemMasterData>();

            var accountMasterData = fixture.Create<AccountMasterData>();
            var outletMasterData = fixture.Create<OutletMasterData>();
            var agencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var posSystemMasterData = fixture.CreateMany<PosSystemMasterData>();

            var mwConfig = GetMiddlewareConfig(confAccountMasterData, confOutletMasterData, confAgencyMasterData, confPosSystemMasterData);
            var accountMasterDataRepoMock = new Mock<IMasterDataRepository<AccountMasterData>>(MockBehavior.Strict);
            accountMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(new[] { accountMasterData });
            var outletMasterDataRepoMock = new Mock<IMasterDataRepository<OutletMasterData>>(MockBehavior.Strict);
            outletMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(new[] { outletMasterData });
            var agencyMasterDataRepoMock = new Mock<IMasterDataRepository<AgencyMasterData>>(MockBehavior.Strict);
            agencyMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(agencyMasterData);
            var posSystemMasterDataRepoMock = new Mock<IMasterDataRepository<PosSystemMasterData>>(MockBehavior.Strict);
            posSystemMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(posSystemMasterData);

            var sut = new MasterDataService(mwConfig, accountMasterDataRepoMock.Object, outletMasterDataRepoMock.Object,
                posSystemMasterDataRepoMock.Object, agencyMasterDataRepoMock.Object);
            var masterData = sut.GetFromConfig();

            masterData.Account.Should().BeEquivalentTo(confAccountMasterData);
            masterData.Outlet.Should().BeEquivalentTo(confOutletMasterData);
            masterData.Agencies.Should().BeEquivalentTo(confAgencyMasterData);
            masterData.PosSystems.Should().BeEquivalentTo(confPosSystemMasterData);
        }


        [Fact]
        public void LoadFromDbOrConfig_NoDBDataAvailable_ExpectConfData()
        {
            var fixture = new Fixture();
            var confAccountMasterData = fixture.Create<AccountMasterData>();
            var confOutletMasterData = fixture.Create<OutletMasterData>();
            var confAgencyMasterData = fixture.CreateMany<AgencyMasterData>();
            var confPosSystemMasterData = fixture.CreateMany<PosSystemMasterData>();

            var mwConfig = GetMiddlewareConfig(confAccountMasterData, confOutletMasterData, confAgencyMasterData, confPosSystemMasterData);
            var accountMasterDataRepoMock = new Mock<IMasterDataRepository<AccountMasterData>>(MockBehavior.Strict);

            accountMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(Enumerable.Empty<AccountMasterData>);
            var outletMasterDataRepoMock = new Mock<IMasterDataRepository<OutletMasterData>>(MockBehavior.Strict);
            outletMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(Enumerable.Empty<OutletMasterData>);
            var agencyMasterDataRepoMock = new Mock<IMasterDataRepository<AgencyMasterData>>(MockBehavior.Strict);
            agencyMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(Enumerable.Empty<AgencyMasterData>);
            var posSystemMasterDataRepoMock = new Mock<IMasterDataRepository<PosSystemMasterData>>(MockBehavior.Strict);
            posSystemMasterDataRepoMock.Setup(x => x.GetAsync()).ReturnsAsync(Enumerable.Empty<PosSystemMasterData>);

            var sut = new MasterDataService(mwConfig, accountMasterDataRepoMock.Object, outletMasterDataRepoMock.Object,
                posSystemMasterDataRepoMock.Object, agencyMasterDataRepoMock.Object);
            var masterData = sut.GetFromConfig();

            masterData.Account.Should().BeEquivalentTo(confAccountMasterData);
            masterData.Outlet.Should().BeEquivalentTo(confOutletMasterData);
            masterData.Agencies.Should().BeEquivalentTo(confAgencyMasterData);
            masterData.PosSystems.Should().BeEquivalentTo(confPosSystemMasterData);
        }




    }
}
