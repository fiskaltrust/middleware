using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{    
    public partial class MasterData : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AccountMasterData",
                c => new
                    {
                        AccountId = c.Guid(nullable: false),
                        AccountName = c.String(),
                        Street = c.String(),
                        Zip = c.String(),
                        City = c.String(),
                        Country = c.String(),
                        TaxId = c.String(),
                        VatId = c.String(),
                    })
                .PrimaryKey(t => t.AccountId, clustered: false);
            
            CreateTable(
                "dbo.AgencyMasterData",
                c => new
                    {
                        AgencyId = c.Guid(nullable: false),
                        Name = c.String(),
                        Street = c.String(),
                        Zip = c.String(),
                        City = c.String(),
                        Country = c.String(),
                        TaxId = c.String(),
                        VatId = c.String(),
                    })
                .PrimaryKey(t => t.AgencyId, clustered: false);
            
            CreateTable(
                "dbo.OutletMasterData",
                c => new
                    {
                        OutletId = c.Guid(nullable: false),
                        OutletName = c.String(),
                        Street = c.String(),
                        Zip = c.String(),
                        City = c.String(),
                        Country = c.String(),
                        VatId = c.String(),
                    })
                .PrimaryKey(t => t.OutletId, clustered: false);
            
            CreateTable(
                "dbo.PosSystemMasterData",
                c => new
                    {
                        PosSystemId = c.Guid(nullable: false),
                        Brand = c.String(),
                        Model = c.String(),
                        SoftwareVersion = c.String(),
                        BaseCurrency = c.String(),
                    })
                .PrimaryKey(t => t.PosSystemId, clustered: false);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.PosSystemMasterData");
            DropTable("dbo.OutletMasterData");
            DropTable("dbo.AgencyMasterData");
            DropTable("dbo.AccountMasterData");
        }
    }
}
