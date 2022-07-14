using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class Indices : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.ftActionJournal", "TimeStamp");
            CreateIndex("dbo.ftJournalAT", "TimeStamp");
            CreateIndex("dbo.ftJournalDE", "TimeStamp");
            CreateIndex("dbo.ftJournalFR", "TimeStamp");
            CreateIndex("dbo.ftQueueItem", "cbReceiptReference");
            CreateIndex("dbo.ftQueueItem", "TimeStamp");
            CreateIndex("dbo.ftReceiptJournal", "TimeStamp");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ftReceiptJournal", new[] { "TimeStamp" });
            DropIndex("dbo.ftQueueItem", new[] { "TimeStamp" });
            DropIndex("dbo.ftQueueItem", new[] { "cbReceiptReference" });
            DropIndex("dbo.ftJournalFR", new[] { "TimeStamp" });
            DropIndex("dbo.ftJournalDE", new[] { "TimeStamp" });
            DropIndex("dbo.ftJournalAT", new[] { "TimeStamp" });
            DropIndex("dbo.ftActionJournal", new[] { "TimeStamp" });
        }
    }
}
