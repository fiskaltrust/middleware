using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
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
                .PrimaryKey(t => t.ftJournalMEId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.ftJournalME");
        }
    }
}
