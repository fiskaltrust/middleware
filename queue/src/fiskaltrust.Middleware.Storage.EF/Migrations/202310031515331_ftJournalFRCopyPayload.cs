namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ftJournalFRCopyPayload : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ftJournalFRCopyPayload",
                c => new
                    {
                        QueueItemId = c.Guid(nullable: false),
                        QueueId = c.Guid(nullable: false),
                        CashBoxIdentification = c.String(),
                        Siret = c.String(),
                        ReceiptId = c.String(),
                        ReceiptMoment = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        CopiedReceiptReference = c.String(),
                        CertificateSerialNumber = c.String(),
                        TimeStamp = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.QueueItemId, clustered: false)
                .Index(t => t.TimeStamp);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.ftJournalFRCopyPayload", new[] { "TimeStamp" });
            DropTable("dbo.ftJournalFRCopyPayload");
        }
    }
}
