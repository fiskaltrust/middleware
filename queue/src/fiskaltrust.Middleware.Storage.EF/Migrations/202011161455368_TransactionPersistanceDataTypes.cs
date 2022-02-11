using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{    
    public partial class TransactionPersistanceDataTypes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FailedFinishTransaction", "TransactionNumber", c => c.Long());
            AddColumn("dbo.OpenTransaction", "TransactionNumber", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.OpenTransaction", "TransactionNumber");
            DropColumn("dbo.FailedFinishTransaction", "TransactionNumber");
        }
    }
}
