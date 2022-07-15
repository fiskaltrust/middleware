using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;

namespace fiskaltrust.Middleware.Storage.EF
{
    public class MiddlewareDbContext : DbContext, IDbModelCacheKeyProvider
    {
#pragma warning disable IDE0032 // Use auto property
        private readonly string _schemaString = null;
#pragma warning restore IDE0032 // Use auto property

        public DbSet<ftCashBox> CashBoxList { get; set; }

        public DbSet<ftQueue> QueueList { get; set; }

        public DbSet<ftQueueAT> QueueATList { get; set; }

        public DbSet<ftQueueDE> QueueDEList { get; set; }

        public DbSet<ftQueueFR> QueueFRList { get; set; }

        public DbSet<ftSignaturCreationUnitAT> SignatureCreationUnitATList { get; set; }

        public DbSet<ftSignaturCreationUnitDE> SignatureCreationUnitDEList { get; set; }

        public DbSet<ftSignaturCreationUnitFR> SignatureCreationUnitFRList { get; set; }

        public DbSet<ftJournalAT> JournalATList { get; set; }

        public DbSet<ftJournalDE> JournalDEList { get; set; }

        public DbSet<ftJournalFR> JournalFRList { get; set; }

        public DbSet<ftQueueItem> QueueItemList { get; set; }

        public DbSet<ftReceiptJournal> ReceiptJournalList { get; set; }

        public DbSet<ftActionJournal> ActionJournalList { get; set; }

        public DbSet<FailedStartTransaction> FailedStartTransactionList { get; set; }

        public DbSet<FailedFinishTransaction> FailedFinishTransactionList { get; set; }

        public DbSet<OpenTransaction> OpenTransactionList { get; set; }

        public DbSet<AccountMasterData> AccountMasterDataList { get; set; }

        public DbSet<OutletMasterData> OutletMasterDataList { get; set; }

        public DbSet<AgencyMasterData> AgencyMasterDataList { get; set; }
        
        public DbSet<PosSystemMasterData> PosSystemMasterDataList { get; set; }

        public string CacheKey => _schemaString;

        public MiddlewareDbContext() : base()
        {
            Database.SetInitializer<MiddlewareDbContext>(null);
        }

        public MiddlewareDbContext(string connectionString, Guid queueId) : base(connectionString)
        {
            Database.SetInitializer<MiddlewareDbContext>(null);
            _schemaString = queueId.ToString("D");
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            if (!string.IsNullOrWhiteSpace(_schemaString))
            {
                modelBuilder.HasDefaultSchema(_schemaString);
            }

            modelBuilder.Properties<DateTime>().Configure(c => c.HasColumnType("datetime2"));
            modelBuilder.Properties<DateTime?>().Configure(c => c.HasColumnType("datetime2"));

            modelBuilder.Entity<ftCashBox>().ToTable(nameof(ftCashBox));
            modelBuilder.Entity<ftCashBox>().HasKey(x => x.ftCashBoxId, x => x.IsClustered(false));

            modelBuilder.Entity<ftQueue>().ToTable(nameof(ftQueue));
            modelBuilder.Entity<ftQueue>().HasKey(x => x.ftQueueId, x => x.IsClustered(false));

            modelBuilder.Entity<ftQueueAT>().ToTable(nameof(ftQueueAT));
            modelBuilder.Entity<ftQueueAT>().HasKey(x => x.ftQueueATId, x => x.IsClustered(false));

            modelBuilder.Entity<ftQueueDE>().ToTable(nameof(ftQueueDE));
            modelBuilder.Entity<ftQueueDE>().HasKey(x => x.ftQueueDEId, x => x.IsClustered(false));

            modelBuilder.Entity<ftQueueFR>().ToTable(nameof(ftQueueFR));
            modelBuilder.Entity<ftQueueFR>().HasKey(x => x.ftQueueFRId, x => x.IsClustered(false));

            modelBuilder.Entity<ftSignaturCreationUnitAT>().ToTable(nameof(ftSignaturCreationUnitAT));
            modelBuilder.Entity<ftSignaturCreationUnitAT>().HasKey(x => x.ftSignaturCreationUnitATId, x => x.IsClustered(false));

            modelBuilder.Entity<ftSignaturCreationUnitDE>().ToTable(nameof(ftSignaturCreationUnitDE));
            modelBuilder.Entity<ftSignaturCreationUnitDE>().HasKey(x => x.ftSignaturCreationUnitDEId, x => x.IsClustered(false));

            modelBuilder.Entity<ftSignaturCreationUnitFR>().ToTable(nameof(ftSignaturCreationUnitFR));
            modelBuilder.Entity<ftSignaturCreationUnitFR>().HasKey(x => x.ftSignaturCreationUnitFRId, x => x.IsClustered(false));

            modelBuilder.Entity<ftJournalAT>().ToTable(nameof(ftJournalAT));
            modelBuilder.Entity<ftJournalAT>().HasKey(x => x.ftJournalATId, x => x.IsClustered(false));
            modelBuilder.Entity<ftJournalAT>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftJournalDE>().ToTable(nameof(ftJournalDE));
            modelBuilder.Entity<ftJournalDE>().HasKey(x => x.ftJournalDEId, x => x.IsClustered(false));
            modelBuilder.Entity<ftJournalDE>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftJournalFR>().ToTable(nameof(ftJournalFR));
            modelBuilder.Entity<ftJournalFR>().HasKey(x => x.ftJournalFRId, x => x.IsClustered(false));
            modelBuilder.Entity<ftJournalFR>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftQueueItem>().ToTable(nameof(ftQueueItem));
            modelBuilder.Entity<ftQueueItem>().HasKey(x => x.ftQueueItemId, x => x.IsClustered(false));
            modelBuilder.Entity<ftQueueItem>().Property(x => x.cbReceiptReference).HasMaxLength(450);
            modelBuilder.Entity<ftQueueItem>().HasIndex(x => x.cbReceiptReference);
            modelBuilder.Entity<ftQueueItem>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftReceiptJournal>().ToTable(nameof(ftReceiptJournal));
            modelBuilder.Entity<ftReceiptJournal>().HasKey(x => x.ftReceiptJournalId, x => x.IsClustered(false));
            modelBuilder.Entity<ftReceiptJournal>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<ftActionJournal>().ToTable(nameof(ftActionJournal));
            modelBuilder.Entity<ftActionJournal>().HasKey(x => x.ftActionJournalId, x => x.IsClustered(false));
            modelBuilder.Entity<ftActionJournal>().HasIndex(x => x.TimeStamp);

            modelBuilder.Entity<FailedStartTransaction>().ToTable(nameof(FailedStartTransaction));
            modelBuilder.Entity<FailedStartTransaction>().HasKey(x => x.cbReceiptReference, x => x.IsClustered(false));

            modelBuilder.Entity<FailedFinishTransaction>().ToTable(nameof(FailedFinishTransaction));
            modelBuilder.Entity<FailedFinishTransaction>().HasKey(x => x.cbReceiptReference, x => x.IsClustered(false));

            modelBuilder.Entity<OpenTransaction>().ToTable(nameof(OpenTransaction));
            modelBuilder.Entity<OpenTransaction>().HasKey(x => x.cbReceiptReference, x => x.IsClustered(false));

            modelBuilder.Entity<AccountMasterData>().ToTable(nameof(AccountMasterData));
            modelBuilder.Entity<AccountMasterData>().HasKey(x => x.AccountId, x => x.IsClustered(false));

            modelBuilder.Entity<OutletMasterData>().ToTable(nameof(OutletMasterData));
            modelBuilder.Entity<OutletMasterData>().HasKey(x => x.OutletId, x => x.IsClustered(false));

            modelBuilder.Entity<AgencyMasterData>().ToTable(nameof(AgencyMasterData));
            modelBuilder.Entity<AgencyMasterData>().HasKey(x => x.AgencyId, x => x.IsClustered(false));

            modelBuilder.Entity<PosSystemMasterData>().ToTable(nameof(PosSystemMasterData));
            modelBuilder.Entity<PosSystemMasterData>().HasKey(x => x.PosSystemId, x => x.IsClustered(false));

            base.OnModelCreating(modelBuilder);
        }
    }
}