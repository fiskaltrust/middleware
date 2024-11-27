namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class FixProcessingVersionIndex : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftQueueItem", "ProcessingVersion", c => c.String(maxLength: 450));
            CreateIndex("dbo.ftQueueItem", "ProcessingVersion");
        }

        public override void Down()
        {
            DropIndex("dbo.ftQueueItem", new[] { "ProcessingVersion" });
            DropColumn("dbo.ftQueueItem", "ProcessingVersion");
        }
    }
}
