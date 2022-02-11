using System;
using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class FailedFinishTransactionRequest : DbMigration
    {
        public override void Up() => AddColumn("dbo.FailedFinishTransaction", "Request", c => c.String());

        public override void Down() => DropColumn("dbo.FailedFinishTransaction", "Request");
    }
}
