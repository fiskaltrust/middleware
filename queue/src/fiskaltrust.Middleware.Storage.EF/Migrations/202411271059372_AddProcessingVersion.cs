namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProcessingVersion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftQueueItem", "ProcessingVersion", c => c.String());
            CreateIndex("dbo.ftQueueItem", "ProcessingVersion");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ftQueueItem", new[] { "ProcessingVersion" });
            DropColumn("dbo.ftQueueItem", "ProcessingVersion");
        }
    }
}
