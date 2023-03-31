using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class Italy : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ftJournalIT",
                c => new
                    {
                        ftJournalITId = c.Guid(nullable: false),
                        ftQueueItemId = c.Guid(nullable: false),
                        ftQueueId = c.Guid(nullable: false),
                        ftSignaturCreationUnitITId = c.Guid(nullable: false),
                        ReceiptNumber = c.Long(nullable: false),
                        ZRepNumber = c.Long(nullable: false),
                        JournalType = c.Long(nullable: false),
                        cbReceiptReference = c.String(),
                        DataJson = c.String(),
                        ReceiptDateTime = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftJournalITId, clustered: false)
                .Index(t => t.cbReceiptReference)
                .Index(t => t.TimeStamp);
            
            CreateTable(
                "dbo.ftQueueIT",
                c => new
                    {
                        ftQueueITId = c.Guid(nullable: false),
                        ftSignaturCreationUnitITId = c.Guid(),
                        LastHash = c.String(),
                        CashBoxIdentification = c.String(),
                        SSCDFailCount = c.Int(nullable: false),
                        SSCDFailMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        SSCDFailQueueItemId = c.Guid(),
                        UsedFailedCount = c.Int(nullable: false),
                        UsedFailedMomentMin = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedMomentMax = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedQueueItemId = c.Guid(),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftQueueITId, clustered: false);
            
            CreateTable(
                "dbo.ftSignaturCreationUnitIT",
                c => new
                    {
                        ftSignaturCreationUnitITId = c.Guid(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                        Url = c.String(),
                        InfoJson = c.String(),
                    })
                .PrimaryKey(t => t.ftSignaturCreationUnitITId, clustered: false);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.ftJournalIT", new[] { "TimeStamp" });
            DropIndex("dbo.ftJournalIT", new[] { "cbReceiptReference" });
            DropTable("dbo.ftSignaturCreationUnitIT");
            DropTable("dbo.ftQueueIT");
            DropTable("dbo.ftJournalIT");
        }
    }
}
