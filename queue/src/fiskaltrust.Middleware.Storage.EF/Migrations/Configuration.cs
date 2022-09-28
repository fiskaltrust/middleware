using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<MiddlewareDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            CommandTimeout = 30 * 60;
        }

        protected override void Seed(MiddlewareDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
        }
    }
}
