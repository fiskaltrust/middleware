namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    /// market-fr#235: the queue row of a copy journaled by QueueFR v2, so the copy's queue item is read
    /// without a scan in any storage. Null on rows written by v1.2.
    /// </summary>
    public partial class JournalFRCopyPayloadQueueRow : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftJournalFRCopyPayload", "QueueRow", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ftJournalFRCopyPayload", "QueueRow");
        }
    }
}
