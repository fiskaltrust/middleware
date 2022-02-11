using System;

namespace fiskaltrust.Middleware.Storage.EFCore.AcceptanceTest.SQLServer.Fixtures
{
    public static class EFCoreSqlServerConnectionStringFixture
    {
        public const string ServerConnectionString = @"Data Source=(LocalDb)\MSSQLLocalDB;Integrated Security=SSPI";
        public static string DatabaseName { get; } = $"EntityFrameworkLocalDatabase{Guid.NewGuid().ToString().Replace("-", "")}";
        public static string DatabaseConnectionString { get; } = $@"Data Source=(LocalDb)\MSSQLLocalDB;Initial Catalog={DatabaseName};Integrated Security=SSPI";
    }
}
