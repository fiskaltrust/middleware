using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.storage.serialization.V0;
using Newtonsoft.Json;
using NuGet;

namespace fiskaltrust.Middleware.Queue.FunctionalTest.Helper
{
    public static class LauncherPreparationHelper
    {
        public const string PACKAGES_CASHBOXID = "346e2de1-a973-e611-80ea-5065f38adae1";
        public const string PACKAGES_ACCESSTOKEN = "BL/Yb+4fkMBqupvqU9AqnehyAPe9+XVnXjerxShkg3w2ubILVxTEkRFppSYHwCODDwPBkMI9hlVdVOfCjiRsWg4=";

        public static async Task<(Process launcherProcess, string grpcQueueEndpoint, string wcfQueueEndpoint)> PrepareOfflineLauncher(ftCashBoxConfiguration cashBoxConfiguration)
        {
            cashBoxConfiguration.TimeStamp = DateTime.UtcNow.Ticks;
            var queueConfiguration = cashBoxConfiguration.ftQueues[0];
            var scuConfiguration = cashBoxConfiguration.ftSignaturCreationDevices[0];

            var baseDirectory = Path.Combine(Path.GetTempPath(), cashBoxConfiguration.ftCashBoxId.ToString());
            if (Directory.Exists(baseDirectory))
            {
                Directory.Delete(baseDirectory, true);
            }
            Directory.CreateDirectory(baseDirectory);
            var offlineLauncher = Path.Combine(baseDirectory, "Launcher");
            Directory.CreateDirectory(offlineLauncher);
            var offlineLauncherPackages = Path.Combine(offlineLauncher, "Packages");
            Directory.CreateDirectory(offlineLauncherPackages);

            var localFeedDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(localFeedDirectory);

            CopyAllFiles(Path.Combine(Directory.GetCurrentDirectory(), "Packages"), localFeedDirectory, "*.nupkg");

            var packageDownloader = new PackageDownloader(localFeedDirectory, "https://packages-sandbox.fiskaltrust.cloud", Guid.Parse(PACKAGES_CASHBOXID), PACKAGES_ACCESSTOKEN);
            var queuePackage = packageDownloader.GetFromLocalFeed(queueConfiguration.Package);
            queueConfiguration.Version = queuePackage.Version.ToString();
            packageDownloader.LoadNugetPackages(queuePackage);
            packageDownloader.LoadNugetPackages(packageDownloader.GetFromFiskaltrustFeed(scuConfiguration.Package, new SemanticVersion(scuConfiguration.Version)));

            CopyAllFiles(localFeedDirectory, offlineLauncherPackages, "*.nupkg");
            await packageDownloader.InstallLauncherAsync(offlineLauncher);
            File.WriteAllText(Path.Combine(offlineLauncher, "configuration.json"), JsonConvert.SerializeObject(cashBoxConfiguration));
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(Path.Combine(offlineLauncher, "fiskaltrust.exe"), $"-cashboxid={cashBoxConfiguration.ftCashBoxId} -sandbox  -test -useoffline -verbosity=Debug")
            };
            process.Start();
            return (process, queueConfiguration.Url[0], queueConfiguration.Url[1]);
        }

        public static void CopyAllFiles(string sourceDir, string targetDir, string filter)
        {
            foreach (var file in Directory.GetFiles(sourceDir, filter))
            {
                var targetFilename = Path.Combine(targetDir, Path.GetFileName(file));
                if (!File.Exists(targetFilename))
                {
                    File.Copy(file, targetFilename);
                }
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                CopyAllFiles(directory, targetDir, filter);
            }
        }
    }
}