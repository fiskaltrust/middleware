using System;

namespace fiskaltrust.Middleware.Storage.MySQL.AcceptanceTest.Fixtures
{
    public static class MySQLConnectionStringFixture
    {
        public static Guid QueueId = Guid.NewGuid();

        public static readonly string ServerConnectionString =
            "Server=localhost;Uid=root;Pwd=mysecretpassword;"; //Environment.GetEnvironmentVariable("CONNECTIONSTRING_MYSQL_TESTS");
        public static string DatabaseName { get; } = QueueId.ToString().Replace("-", string.Empty);
        public static string DatabaseConnectionString { get; } = $"{ServerConnectionString}database={DatabaseName}";
    }
}
