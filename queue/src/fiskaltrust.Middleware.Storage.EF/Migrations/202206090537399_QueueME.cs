namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class QueueME : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ftJournalME",
                c => new
                    {
                        ftJournalMEId = c.Guid(nullable: false),
                        cbReference = c.String(),
                        ftInvoiceNumber = c.String(),
                        ftOrdinalNumber = c.Int(nullable: false),
                        ftQueueItemId = c.Guid(nullable: false),
                        ftQueueId = c.Guid(nullable: false),
                        Number = c.Long(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                        IIC = c.String(),
                        FIC = c.String(),
                        FCDC = c.String(),
                        JournalType = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftJournalMEId, clustered: false);
            
            CreateTable(
                "dbo.ftQueueME",
                c => new
                    {
                        ftQueueMEId = c.Guid(nullable: false),
                        ftSignaturCreationUnitMEId = c.Guid(),
                        LastHash = c.String(),
                        SSCDFailCount = c.Int(nullable: false),
                        SSCDFailMoment = c.DateTime(precision: 7, storeType: "datetime2"),
                        SSCDFailQueueItemId = c.Guid(),
                        UsedFailedCount = c.Int(nullable: false),
                        UsedFailedMomentMin = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedMomentMax = c.DateTime(precision: 7, storeType: "datetime2"),
                        UsedFailedQueueItemId = c.Guid(),
                        DailyClosingNumber = c.Int(nullable: false),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ftQueueMEId, clustered: false);
            
            CreateTable(
                "dbo.ftSignaturCreationUnitME",
                c => new
                    {
                        ftSignaturCreationUnitMEId = c.Guid(nullable: false),
                        Url = c.String(),
                        TimeStamp = c.Long(nullable: false),
                        TseInfoJson = c.String(),
                        Mode = c.Int(nullable: false),
                        ModeConfigurationJson = c.String(),
                        IssuerTin = c.String(),
                        BusinessUnitCode = c.String(),
                        TcrIntId = c.String(),
                        SoftwareCode = c.String(),
                        MaintainerCode = c.String(),
                        ValidFrom = c.DateTime(precision: 7, storeType: "datetime2"),
                        ValidTo = c.DateTime(precision: 7, storeType: "datetime2"),
                        EnuType = c.String(),
                        TcrCode = c.String(),
                    })
                .PrimaryKey(t => t.ftSignaturCreationUnitMEId, clustered: false);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ftSignaturCreationUnitME");
            DropTable("dbo.ftQueueME");
            DropTable("dbo.ftJournalME");
        }
    }
}
