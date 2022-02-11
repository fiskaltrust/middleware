using System;
using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    public partial class TransactionPersistence : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FailedFinishTransaction",
                c => new
                    {
                        cbReceiptReference = c.String(nullable: false, maxLength: 128),
                        FinishMoment = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                        ftQueueItemId = c.Guid(nullable: false),
                        CashBoxIdentification = c.String(),
                    })
                .PrimaryKey(t => t.cbReceiptReference, clustered: false);
            
            CreateTable(
                "dbo.FailedStartTransaction",
                c => new
                    {
                        cbReceiptReference = c.String(nullable: false, maxLength: 128),
                        ftQueueItemId = c.Guid(nullable: false),
                        CashBoxIdentification = c.String(),
                        Request = c.String(),
                        StartMoment = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.cbReceiptReference, clustered: false);
            
            CreateTable(
                "dbo.OpenTransaction",
                c => new
                    {
                        cbReceiptReference = c.String(nullable: false, maxLength: 128),
                        StartTransactionSignatureBase64 = c.String(),
                        StartMoment = c.DateTime(nullable: false, precision: 7, storeType: "datetime2"),
                    })
                .PrimaryKey(t => t.cbReceiptReference, clustered: false);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.OpenTransaction");
            DropTable("dbo.FailedStartTransaction");
            DropTable("dbo.FailedFinishTransaction");
        }
    }
}
