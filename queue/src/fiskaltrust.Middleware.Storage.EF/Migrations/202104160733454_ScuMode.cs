using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class ScuMode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftSignaturCreationUnitDE", "Mode", c => c.Int(nullable: false));
            AddColumn("dbo.ftSignaturCreationUnitDE", "ModeConfigurationJson", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.ftSignaturCreationUnitDE", "ModeConfigurationJson");
            DropColumn("dbo.ftSignaturCreationUnitDE", "Mode");
        }
    }
}
