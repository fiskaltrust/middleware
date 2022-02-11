using System;
using MySqlConnector;
using Xunit;

namespace fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest.Fixtures
{
    public class MySQLStorageCollectionFixture : IDisposable
    {
        public const string CollectionName = "MySqlStorageCollection";

        public MySQLStorageCollectionFixture()
        {

        }
        public void Dispose() => DropDatabaseIfExists(MySQLConnectionStringFixture.ServerConnectionString, MySQLConnectionStringFixture.DatabaseName);
        public void DropDatabaseIfExists(string connectionString, string databaseName)
        {
            using (var mySqlConnetion = new MySqlConnection(connectionString))
            {
                mySqlConnetion.Open();
                using (var command = new MySqlCommand($@"DROP DATABASE IF EXISTS `{databaseName}`", mySqlConnetion))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    [CollectionDefinition(MySQLStorageCollectionFixture.CollectionName)]
    public class DatabaseCollection<T> : ICollectionFixture<MySQLStorageCollectionFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
        //regarding to my sql, this fixture is just for grouping each test class together.
        //since its mendatory to call dispose after test, it doesnt let xunit to run test methods in diffrent test classes parallel
    }
}
