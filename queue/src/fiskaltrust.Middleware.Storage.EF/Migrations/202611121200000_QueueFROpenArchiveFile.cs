namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    /// <summary>
    /// market-fr ADR 0018: the archive file a QueueFR v2 queue keeps open, as JSON.
    /// </summary>
    public partial class QueueFROpenArchiveFile : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftQueueFR", "OpenArchiveFile", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.ftQueueFR", "OpenArchiveFile");
        }
    }
}
