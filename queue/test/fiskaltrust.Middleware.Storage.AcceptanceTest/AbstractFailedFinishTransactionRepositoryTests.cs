﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.Middleware.Contracts.Data;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Storage.AcceptanceTest
{
    public abstract class AbstractFailedFinishTransactionRepositoryTests : IDisposable
    {
        public abstract Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateRepository(IEnumerable<FailedFinishTransaction> entries);
        public abstract Task<IPersistentTransactionRepository<FailedFinishTransaction>> CreateReadOnlyRepository(IEnumerable<FailedFinishTransaction> entries);

        public virtual void DisposeDatabase() { return; }

        public void Dispose() => DisposeDatabase();

        [Fact]
        public async Task GetAsync_ShouldReturnAllTransactionsThatExistInRepository()
        {
            var expectedEntry = StorageTestFixtureProvider.GetFixture().CreateMany<FailedFinishTransaction>(10);

            var sut = await CreateReadOnlyRepository(expectedEntry);
            var actualEntry = await sut.GetAsync();

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_TheElementWithTheGivenId_IfTheIdExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<FailedFinishTransaction>(10).ToList();
            var expectedEntry = entries[4];

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(expectedEntry.cbReceiptReference);

            actualEntry.Should().BeEquivalentTo(expectedEntry);
        }

        [Fact]
        public async Task GetAsync_WithId_ShouldReturn_Null_IfTheGivenIdDoesntExist()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<FailedFinishTransaction>(10).ToList();

            var sut = await CreateReadOnlyRepository(entries);
            var actualEntry = await sut.GetAsync(string.Empty);

            actualEntry.Should().BeNull();
        }

        [Fact]
        public async Task InsertAsync_ShouldAddEntry_ToTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<FailedFinishTransaction>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<FailedFinishTransaction>();

            var sut = await CreateRepository(entries);
            await sut.InsertOrUpdateTransactionAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.cbReceiptReference);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task InsertAsync_ShouldUpdateEntry_IfEntryAlreadyExists()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<FailedFinishTransaction>(10).ToList();
            var entryToInsert = StorageTestFixtureProvider.GetFixture().Create<FailedFinishTransaction>();
            entryToInsert.cbReceiptReference = entries[0].cbReceiptReference;

            var sut = await CreateRepository(entries);
            await sut.InsertOrUpdateTransactionAsync(entryToInsert);

            var insertedEntry = await sut.GetAsync(entryToInsert.cbReceiptReference);
            insertedEntry.Should().BeEquivalentTo(entryToInsert);
        }

        [Fact]
        public async Task RemoveAsync_ShouldRemoveEntry_FromTheDatabase()
        {
            var entries = StorageTestFixtureProvider.GetFixture().CreateMany<FailedFinishTransaction>(10).ToList();

            var sut = await CreateRepository(entries);
            await sut.RemoveAsync(entries[0].cbReceiptReference);

            var deletedEntry = await sut.GetAsync(entries[0].cbReceiptReference);
            deletedEntry.Should().BeNull();
        }
    }
}
