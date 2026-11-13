namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    /// <summary>
    /// market-fr ADR 0018: the QueueFR v2 state that has no v1 column, as JSON.
    /// </summary>
    public partial class QueueFRStateV2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftQueueFR", "StateV2", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.ftQueueFR", "StateV2");
        }
    }
}
