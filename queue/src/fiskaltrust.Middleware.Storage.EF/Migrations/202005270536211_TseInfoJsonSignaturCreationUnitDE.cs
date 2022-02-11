using System;
using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class TseInfoJsonSignaturCreationUnitDE : DbMigration
    {
        public override void Up() => AddColumn("dbo.ftSignaturCreationUnitDE", "TseInfoJson", c => c.String());

        public override void Down() => DropColumn("dbo.ftSignaturCreationUnitDE", "TseInfoJson");
    }
}
