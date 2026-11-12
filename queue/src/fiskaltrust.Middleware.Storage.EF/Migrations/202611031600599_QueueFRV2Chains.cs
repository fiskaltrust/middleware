namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class QueueFRV2Chains : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftQueueFR", "ENumerator", c => c.Long(nullable: false));
            AddColumn("dbo.ftQueueFR", "SNumerator", c => c.Long(nullable: false));
            AddColumn("dbo.ftQueueFR", "ZNumerator", c => c.Long(nullable: false));
            AddColumn("dbo.ftQueueFR", "MNumerator", c => c.Long(nullable: false));
            AddColumn("dbo.ftQueueFR", "YNumerator", c => c.Long(nullable: false));
            AddColumn("dbo.ftQueueFR", "TLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "PLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "ILastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "BLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "LLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "CLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "XLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "ELastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "SLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "ZLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "MLastSignature", c => c.String());
            AddColumn("dbo.ftQueueFR", "YLastSignature", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ftQueueFR", "YLastSignature");
            DropColumn("dbo.ftQueueFR", "MLastSignature");
            DropColumn("dbo.ftQueueFR", "ZLastSignature");
            DropColumn("dbo.ftQueueFR", "SLastSignature");
            DropColumn("dbo.ftQueueFR", "ELastSignature");
            DropColumn("dbo.ftQueueFR", "XLastSignature");
            DropColumn("dbo.ftQueueFR", "CLastSignature");
            DropColumn("dbo.ftQueueFR", "LLastSignature");
            DropColumn("dbo.ftQueueFR", "BLastSignature");
            DropColumn("dbo.ftQueueFR", "ILastSignature");
            DropColumn("dbo.ftQueueFR", "PLastSignature");
            DropColumn("dbo.ftQueueFR", "TLastSignature");
            DropColumn("dbo.ftQueueFR", "YNumerator");
            DropColumn("dbo.ftQueueFR", "MNumerator");
            DropColumn("dbo.ftQueueFR", "ZNumerator");
            DropColumn("dbo.ftQueueFR", "SNumerator");
            DropColumn("dbo.ftQueueFR", "ENumerator");
        }
    }
}
