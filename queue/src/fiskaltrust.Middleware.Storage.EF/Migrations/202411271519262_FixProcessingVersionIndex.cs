namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class FixProcessingVersionIndex : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftQueueItem", "ProcessingVersion", c => c.String(maxLength: 450));
        }

        public override void Down()
        {
            DropColumn("dbo.ftQueueItem", "ProcessingVersion");
        }
    }
}
