using System.Diagnostics;
using System.IO;
using Xunit;

#if DEBUG

namespace fiskaltrust.Middleware.Queue.FunctionalTest
{
    public class LocalNugetPreparationTests
    {
        [Fact]
        public void Prepare_Ef_Queue_Package() => PerformPackForProject("fiskaltrust.Middleware.Queue.EF");

        [Fact]
        public void Prepare_SQLite_Queue_Package() => PerformPackForProject("fiskaltrust.Middleware.Queue.SQLite");

        [Fact]
        public void Prepare_InMemory_Queue_Package() => PerformPackForProject("fiskaltrust.Middleware.Queue.InMemory");

        private static void PerformPackForProject(string project)
        {
            var queueProjectFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", project);
            var queueTestProjectFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "test", "fiskaltrust.Middleware.Queue.FunctionalTest", "Packages");

            var processStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = queueProjectFolder,
                FileName = "dotnet",
                Arguments = $"pack --output {queueTestProjectFolder} --configuration Release"
            };

            Process.Start(processStartInfo).WaitForExit();
        }
    }
}

#endif