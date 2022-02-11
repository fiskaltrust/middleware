using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.Middleware.Contracts.Repositories;
using FluentAssertions;
using Xunit;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public abstract class AbstractPosSystemMasterDataRepositoryTests : IDisposable
    {
        public abstract Task<IMasterDataRepository<PosSystemMasterData>> CreateRepository(IEnumerable<PosSystemMasterData> entries);
        public abstract Task<IMasterDataRepository<PosSystemMasterData>> CreateReadOnlyRepository(IEnumerable<PosSystemMasterData> entries);

        public virtual void DisposeDatabase() { return; }

        public void Dispose() => DisposeDatabase();

        [Fact]
        public async Task GetAsync_ShouldReturnAllEntitiesThatExistInRepository()
        {
            var expectedEntries = StorageTestFixtureProvider.GetFixture().CreateMany<PosSystemMasterData>(10);

            var sut = await CreateReadOnlyRepository(expectedEntries);
            var actualEntries = await sut.GetAsync();

            actualEntries.Should().BeEquivalentTo(expectedEntries);
        }

        [Fact]
        public async Task GetAsync_ShouldNotReturnNull()
        {
            var sut = await CreateReadOnlyRepository(new List<PosSystemMasterData>());
            var actualEntries = await sut.GetAsync();

            actualEntries.Should().NotBeNull();
            actualEntries.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<PosSystemMasterData>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<PosSystemMasterData>();

            var sut = await CreateRepository(entries);
            await sut.CreateAsync(entryToInsert);

            var actualEntries = await sut.GetAsync();
            actualEntries.Should().HaveCount(11);
            actualEntries.Should().ContainEquivalentOf(entryToInsert);
        }

        [Fact]
        public async Task ClearAsync_ShouldRemoveAlllEntries_FromTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<PosSystemMasterData>(1).ToList();

            var sut = await CreateRepository(entries);
            await sut.ClearAsync();
            var actualEntries = await sut.GetAsync();

            actualEntries.Should().NotBeNull();
            actualEntries.Should().BeEmpty();
        }
    }
}
