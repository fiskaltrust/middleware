using System;
using System.Data.SqlClient;
using fiskaltrust.Middleware.Storage.Ef;
using Xunit;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest.Fixtures
{
    public class EfStorageCollectionFixture : IDisposable
    {
        public const string CollectionName = "EntityFrameworkStorageCollection";

        public EfStorageCollectionFixture()
        {
            DropDatabaseIfExists(EfConnectionStringFixture.ServerConnectionString, EfConnectionStringFixture.DatabaseName);
        }

        private void DropDatabaseIfExists(string connectionString, string databaseName)
        {
            using (var sqlConnetion = new SqlConnection(connectionString))
            {
                sqlConnetion.Open();
                using (var command = new SqlCommand($@"USE master; IF EXISTS(select * from sys.databases where name='{databaseName}') BEGIN ALTER DATABASE {databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {databaseName} END", sqlConnetion))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Dispose() { } // => DropDatabaseIfExists(EfConnectionStringFixture.ServerConnectionString, EfConnectionStringFixture.DatabaseName);
    }

    [CollectionDefinition(EfStorageCollectionFixture.CollectionName)]
    public class DatabaseCollection<T> : ICollectionFixture<EfStorageCollectionFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
