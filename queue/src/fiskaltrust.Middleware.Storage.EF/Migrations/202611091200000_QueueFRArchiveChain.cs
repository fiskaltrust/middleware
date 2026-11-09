namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    /// <summary>
    /// market-fr#221: the previous signature of the QueueFR v2 chain of archive receipts (A). Its
    /// numerator and totals are the v1 A columns.
    /// </summary>
    public partial class QueueFRArchiveChain : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftQueueFR", "ALastSignature", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.ftQueueFR", "ALastSignature");
        }
    }
}
