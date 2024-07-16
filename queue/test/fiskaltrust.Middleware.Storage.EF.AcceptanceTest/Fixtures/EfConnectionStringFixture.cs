using System;

namespace fiskaltrust.Middleware.Storage.EF.AcceptanceTest.Fixtures
{
    public static class EfConnectionStringFixture
    {
        public const string ServerConnectionString = "Server=localhost,1433;MultipleActiveResultSets=true;User ID=sa;Password=my-secret-PW-01";// ?? @"Data Source=(LocalDb)\MSSQLLocalDB;Integrated Security=SSPI;Connect Timeout=60;";
        public static string DatabaseName { get; } = $"EntityFrameworkLocalDatabase{Guid.NewGuid().ToString().Replace("-", "")}";
        public static string DatabaseConnectionString { get; } = $"{ServerConnectionString};Initial Catalog={DatabaseName};";        
    }
}
