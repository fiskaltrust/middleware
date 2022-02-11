using System;
using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class DailyClosingNumber : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ftQueueDE", "DailyClosingNumber", c => c.Int(nullable: false, defaultValue: 0));

            if (MigrationQueueIdProvider.QueueId != Guid.Empty)
            {
                var sql = $@"UPDATE [{MigrationQueueIdProvider.QueueId}].ftQueueDE SET DailyClosingNumber =
(
    SELECT COUNT(*) from [{MigrationQueueIdProvider.QueueId}].ftActionJournal
    WHERE ftActionJournal.ftQueueId = ftQueueDE.ftQueueDEId 
    AND (ftActionJournal.Type = '4445000008000007' OR ftActionJournal.Type = '4445000000000007')
)
WHERE DailyClosingNumber = 0";
                Sql(sql);
            }
        }

        public override void Down() => DropColumn("dbo.ftQueueDE", "DailyClosingNumber");
    }
}
