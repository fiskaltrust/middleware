using System.Data.Entity.Migrations;

namespace fiskaltrust.Middleware.Storage.EF.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<MiddlewareDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(MiddlewareDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
        }
    }
}
