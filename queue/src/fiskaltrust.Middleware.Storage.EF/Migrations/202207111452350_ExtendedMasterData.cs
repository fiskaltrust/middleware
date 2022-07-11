using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class ExtendedMasterData : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OutletMasterData", "LocationId", c => c.String());
            AddColumn("dbo.PosSystemMasterData", "Type", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.PosSystemMasterData", "Type");
            DropColumn("dbo.OutletMasterData", "LocationId");
        }
    }
}
