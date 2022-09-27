using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class MECleanup : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftJournalME", "InvoiceNumber", c => c.String());
            AddColumn("dbo.ftJournalME", "YearlyOrdinalNumber", c => c.Int(nullable: false));
            DropColumn("dbo.ftJournalME", "ftInvoiceNumber");
            DropColumn("dbo.ftJournalME", "ftOrdinalNumber");
            DropColumn("dbo.ftSignaturCreationUnitME", "TseInfoJson");
            DropColumn("dbo.ftSignaturCreationUnitME", "Mode");
            DropColumn("dbo.ftSignaturCreationUnitME", "ModeConfigurationJson");
            DropColumn("dbo.ftSignaturCreationUnitME", "EnuType");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ftSignaturCreationUnitME", "EnuType", c => c.String());
            AddColumn("dbo.ftSignaturCreationUnitME", "ModeConfigurationJson", c => c.String());
            AddColumn("dbo.ftSignaturCreationUnitME", "Mode", c => c.Int(nullable: false));
            AddColumn("dbo.ftSignaturCreationUnitME", "TseInfoJson", c => c.String());
            AddColumn("dbo.ftJournalME", "ftOrdinalNumber", c => c.Int(nullable: false));
            AddColumn("dbo.ftJournalME", "ftInvoiceNumber", c => c.String());
            DropColumn("dbo.ftJournalME", "YearlyOrdinalNumber");
            DropColumn("dbo.ftJournalME", "InvoiceNumber");
        }
    }
}
