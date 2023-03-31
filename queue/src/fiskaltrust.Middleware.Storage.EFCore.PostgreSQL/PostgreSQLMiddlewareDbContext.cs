using Microsoft.EntityFrameworkCore;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace fiskaltrust.Middleware.Storage.EFCore.PostgreSQL
{
    public class PostgreSQLMiddlewareDbContext : MiddlewareDbContext
    {
        private readonly Guid? _queueId;

        public PostgreSQLMiddlewareDbContext(DbContextOptions contextOptions, Guid? queueId = null) : base(contextOptions)
        {
            _queueId = queueId;
        }

        public override void Dispose() => base.Dispose();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (_queueId.HasValue)
            {
                modelBuilder.HasDefaultSchema(_queueId.ToString());
            }

            modelBuilder.Entity<ftCashBox>().ToTable(nameof(ftCashBox));
            modelBuilder.Entity<ftCashBox>().HasKey(x => x.ftCashBoxId);

            modelBuilder.Entity<ftQueue>().ToTable(nameof(ftQueue));
            modelBuilder.Entity<ftQueue>().HasKey(x => x.ftQueueId);

            modelBuilder.Entity<ftQueueAT>().ToTable(nameof(ftQueueAT));
            modelBuilder.Entity<ftQueueAT>().HasKey(x => x.ftQueueATId);

            modelBuilder.Entity<ftQueueDE>().ToTable(nameof(ftQueueDE));
            modelBuilder.Entity<ftQueueDE>().HasKey(x => x.ftQueueDEId);

            modelBuilder.Entity<ftQueueFR>().ToTable(nameof(ftQueueFR));
            modelBuilder.Entity<ftQueueFR>().HasKey(x => x.ftQueueFRId);

            modelBuilder.Entity<ftQueueME>().ToTable(nameof(ftQueueME));
            modelBuilder.Entity<ftQueueME>().HasKey(x => x.ftQueueMEId);

            modelBuilder.Entity<ftQueueIT>().ToTable(nameof(ftQueueIT));
            modelBuilder.Entity<ftQueueIT>().HasKey(x => x.ftQueueITId);

            modelBuilder.Entity<ftSignaturCreationUnitAT>().ToTable(nameof(ftSignaturCreationUnitAT));
            modelBuilder.Entity<ftSignaturCreationUnitAT>().HasKey(x => x.ftSignaturCreationUnitATId);

            modelBuilder.Entity<ftSignaturCreationUnitDE>().ToTable(nameof(ftSignaturCreationUnitDE));
            modelBuilder.Entity<ftSignaturCreationUnitDE>().HasKey(x => x.ftSignaturCreationUnitDEId);

            modelBuilder.Entity<ftSignaturCreationUnitFR>().ToTable(nameof(ftSignaturCreationUnitFR));
            modelBuilder.Entity<ftSignaturCreationUnitFR>().HasKey(x => x.ftSignaturCreationUnitFRId);

            modelBuilder.Entity<ftSignaturCreationUnitME>().ToTable(nameof(ftSignaturCreationUnitME));
            modelBuilder.Entity<ftSignaturCreationUnitME>().HasKey(x => x.ftSignaturCreationUnitMEId);

            modelBuilder.Entity<ftSignaturCreationUnitIT>().ToTable(nameof(ftSignaturCreationUnitIT));
            modelBuilder.Entity<ftSignaturCreationUnitIT>().HasKey(x => x.ftSignaturCreationUnitITId);

            modelBuilder.Entity<ftJournalAT>().ToTable(nameof(ftJournalAT));
            modelBuilder.Entity<ftJournalAT>().HasKey(x => x.ftJournalATId);
            modelBuilder.Entity<ftJournalAT>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftJournalDE>().ToTable(nameof(ftJournalDE));
            modelBuilder.Entity<ftJournalDE>().HasKey(x => x.ftJournalDEId);
            modelBuilder.Entity<ftJournalDE>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftJournalFR>().ToTable(nameof(ftJournalFR));
            modelBuilder.Entity<ftJournalFR>().HasKey(x => x.ftJournalFRId);
            modelBuilder.Entity<ftJournalFR>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftJournalME>().ToTable(nameof(ftJournalME));
            modelBuilder.Entity<ftJournalME>().HasKey(x => x.ftJournalMEId);

            modelBuilder.Entity<ftJournalIT>().ToTable(nameof(ftJournalIT));
            modelBuilder.Entity<ftJournalIT>().HasKey(x => x.ftJournalITId);
            modelBuilder.Entity<ftJournalIT>().HasIndex(x => x.TimeStamp);
            modelBuilder.Entity<ftJournalIT>().HasIndex(x => x.cbReceiptReference);

            modelBuilder.Entity<ftQueueItem>().ToTable(nameof(ftQueueItem));
            modelBuilder.Entity<ftQueueItem>().HasKey(x => x.ftQueueItemId);
            modelBuilder.Entity<ftQueueItem>().HasIndex(x => x.cbReceiptReference);
            modelBuilder.Entity<ftQueueItem>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftReceiptJournal>().ToTable(nameof(ftReceiptJournal));
            modelBuilder.Entity<ftReceiptJournal>().HasKey(x => x.ftReceiptJournalId);
            modelBuilder.Entity<ftReceiptJournal>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftActionJournal>().ToTable(nameof(ftActionJournal));
            modelBuilder.Entity<ftActionJournal>().HasKey(x => x.ftActionJournalId);
            modelBuilder.Entity<ftActionJournal>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<FailedStartTransaction>().ToTable(nameof(FailedStartTransaction));
            modelBuilder.Entity<FailedStartTransaction>().HasKey(x => x.cbReceiptReference); //TODO 

            modelBuilder.Entity<FailedFinishTransaction>().ToTable(nameof(FailedFinishTransaction));
            modelBuilder.Entity<FailedFinishTransaction>().HasKey(x => x.cbReceiptReference); //TODO 

            modelBuilder.Entity<OpenTransaction>().ToTable(nameof(OpenTransaction));
            modelBuilder.Entity<OpenTransaction>().HasKey(x => x.cbReceiptReference); //TODO 

            modelBuilder.Entity<AccountMasterData>().ToTable(nameof(AccountMasterData));
            modelBuilder.Entity<AccountMasterData>().HasKey(x => x.AccountId);

            modelBuilder.Entity<OutletMasterData>().ToTable(nameof(OutletMasterData));
            modelBuilder.Entity<OutletMasterData>().HasKey(x => x.OutletId);

            modelBuilder.Entity<AgencyMasterData>().ToTable(nameof(AgencyMasterData));
            modelBuilder.Entity<AgencyMasterData>().HasKey(x => x.AgencyId);

            modelBuilder.Entity<PosSystemMasterData>().ToTable(nameof(PosSystemMasterData));
            modelBuilder.Entity<PosSystemMasterData>().HasKey(x => x.PosSystemId);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class MiddlewareDbContextFactory : IDesignTimeDbContextFactory<PostgreSQLMiddlewareDbContext>
    {
        public PostgreSQLMiddlewareDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PostgreSQLMiddlewareDbContext>();
            optionsBuilder.UseNpgsql(@"Host=localhost;port=5455;Username=postgres;Password=mysecretpassword;Database=postgres");

            return new PostgreSQLMiddlewareDbContext(optionsBuilder.Options);
        }
    }
}
