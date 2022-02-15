using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Storage.EFCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.PostgreSQL.Fixtures
{
    public class EFCorePostgreSQLStorageCollectionFixture : IDisposable
    {
        public const string CollectionName = "EFCorePostgreSQLStorageCollection";
        private static readonly string DatabaseConnectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING_POSTGRESQL_TESTS");
        public static Guid QueueId { get; } = Guid.NewGuid();
        public static string DatabaseName => "db" + QueueId.ToString().Replace("-", string.Empty).Substring(0, 5);
        public PostgreSQLMiddlewareDbContext Context => CreateContext();
        private readonly List<PostgreSQLMiddlewareDbContext> contextes = new List<PostgreSQLMiddlewareDbContext>();

        public EFCorePostgreSQLStorageCollectionFixture()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PostgreSQLMiddlewareDbContext>();
            optionsBuilder.UseNpgsql(string.Format(DatabaseConnectionString, DatabaseName));
            EFCorePostgreSQLStorageBootstrapper.Update(optionsBuilder.Options, QueueId, Mock.Of<ILogger<IMiddlewareBootstrapper>>());
         
        }
        public PostgreSQLMiddlewareDbContext CreateContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PostgreSQLMiddlewareDbContext>();
            optionsBuilder.UseNpgsql(string.Format(DatabaseConnectionString, DatabaseName));
            var context = new PostgreSQLMiddlewareDbContext(optionsBuilder.Options);
            contextes.Add(context);
            return context;
        }
        public static DbContextOptionsBuilder<PostgreSQLMiddlewareDbContext> contextOptionsBuilder()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PostgreSQLMiddlewareDbContext>();
            optionsBuilder.UseNpgsql(string.Format(DatabaseConnectionString, DatabaseName));
            return optionsBuilder;
        }
        public static async Task TruncateTableAsync(string table)
        {
            var del = string.Format("TRUNCATE TABLE \"{0}\".\"{1}\"", QueueId, table);
            using (var context = new PostgreSQLMiddlewareDbContext(contextOptionsBuilder().Options))
            {
                context.Database.ExecuteSqlRaw(del);
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        public void Dispose()
        {
            foreach(var context in contextes)
            {
                context.Database.EnsureDeleted();
                context.Dispose();
            }
        }
    }

    [CollectionDefinition(EFCorePostgreSQLStorageCollectionFixture.CollectionName)]
    public class DatabaseCollection<T> : ICollectionFixture<EFCorePostgreSQLStorageCollectionFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
