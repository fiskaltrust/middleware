using Microsoft.EntityFrameworkCore;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.Middleware.Storage.EFCore.Helpers;

namespace fiskaltrust.Middleware.Storage.EFCore
{
    public class MiddlewareDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDbFunction(() => JsonExtensions.JsonValue(default, default));
        }

        public override void Dispose() => base.Dispose();
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

        public MiddlewareDbContext() { }

        public MiddlewareDbContext(DbContextOptions contextOptions) : base(contextOptions) { }
    }
}