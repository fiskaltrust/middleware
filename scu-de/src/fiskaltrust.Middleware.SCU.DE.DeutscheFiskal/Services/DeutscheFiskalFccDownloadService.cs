using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services
{
    public class DeutscheFiskalFccDownloadService : IFccDownloadService
    {
        private const int MAX_PROCESS_DURATION_MS = 30 * 1000;
        private const string DOWNLOAD_DIRECTORY = "https://downloads.fiskaltrust.cloud/downloads/fcc/";
        private const string NAMEPREFIX = "fcc-package";
        private int _fccRetry = 0;

        private readonly DeutscheFiskalSCUConfiguration _configuration;
        private readonly ILogger<IFccDownloadService> _logger;
        public Version UsedFCCVersion { get; private set; }

        public DeutscheFiskalFccDownloadService(DeutscheFiskalSCUConfiguration configuration, ILogger<IFccDownloadService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public bool IsInstalled(string fccDirectory) => File.Exists(Path.Combine(fccDirectory, ".fccdata\\install\\fcc-version.dat")) || File.Exists(Path.Combine(fccDirectory, ".fccdata\\fcc-version.dat"));
        public bool IsDownloaded(string fccDirectory) => File.Exists(GetCloudConnectorPath(fccDirectory));
        public bool IsLatestVersion(string fccDirectory, Version latestVersion)
        {
            using (var archive = ZipFile.OpenRead(GetCloudConnectorPath(fccDirectory)))
            {
                var entry = archive.GetEntry("META-INF/MANIFEST.MF");
                using (var sr = new StreamReader(entry.Open()))
                {
                    while (sr.Peek() >= 0)
                    {
                        var line = sr.ReadLine();
                        if (line.Contains("Implementation-Version"))
                        {
                            var version = line.Split(':')[1].Trim();
                            UsedFCCVersion = new Version(version);
                            return UsedFCCVersion >= latestVersion;
                        }
                    }
                }
            }
            _logger.LogWarning("Installed FCC version could not be detected; skipping update.");
            return false;
        }

        public bool IsLatestVersionDat(string fccDirectory, Version latestVersion)
        {
            string version;

            if(File.Exists(Path.Combine(fccDirectory, ".fccdata\\install\\fcc-version.dat")))
            {
                version = File.ReadAllText(Path.Combine(fccDirectory, ".fccdata", "install", "fcc-version.dat"));
            }
            else
            {
                version = File.ReadAllText(Path.Combine(fccDirectory, ".fccdata", "fcc-version.dat"));
            }

            var versionInDat = new Version(version);
            return versionInDat >= latestVersion;
        }

        public async Task LogWarningIfFccPathsDontMatchAsync(string fccDirectory)
        {
            try
            {
                using (var sr = new StreamReader(Path.Combine(fccDirectory, "run_fcc.bat")))
                {
                    var content = await sr.ReadToEndAsync().ConfigureAwait(false);
                    if (!IsPathInRunFccIdent(fccDirectory, content, out var pathInFile))
                    {
                        _logger.LogWarning(
                            "The actual FCC directory does not match the FCC directory set in run_fcc.bat." +
                            "This could indicate that the FCC was moved to another directory after setup, which may lead to errors.\n" +
                            "Actual directory: \"{ActualDirectory}\"\n" +
                            "run_fcc.bat directory: \"{RunFccBatDirectory}\"",
                            fccDirectory,
                            pathInFile);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e.Message);
            }
        }

        public bool IsPathInRunFccIdent(string path, string content, out string pathInFile)
        {
            pathInFile = content.Substring(content.IndexOf("-DFCC_ROOT_DIR=") + "-DFCC_ROOT_DIR=".Length);
            var pathend = pathInFile.IndexOf("\"") > 0 && pathInFile.IndexOf("\"") < pathInFile.IndexOf(" ") ? pathInFile.IndexOf("\"") : pathInFile.IndexOf(" ");
            pathInFile = pathInFile.Substring(0, pathend);
            pathInFile = pathInFile.Replace("\"", "");

            path = DeleteLastSlash(path.Trim()).Replace('\\', '/');
            pathInFile = DeleteLastSlash(pathInFile.Trim()).Replace('\\', '/');
            return string.Equals(path, pathInFile, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> DownloadFccAsync(string fccDirectory, Version currentlyInstalledVersion)
        {
            _logger.LogWarning("Downloading and extracting FCC - this will take some time, depending on your internet connection.");
            var tempZipPath = Path.GetTempFileName();
            try
            {
                try
                {
                    await DownloadAndExtractAsync(tempZipPath, fccDirectory, currentlyInstalledVersion).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    if (_configuration.FccRetry > _fccRetry)
                    {
                        await Task.Delay(1000);
                        _fccRetry++;
                        _logger.LogWarning($"An error occured while downloading ({ex.InnerException?.Message ?? ex.Message}), retry {_fccRetry} from {_configuration.FccRetry}");
                        return await DownloadFccAsync(fccDirectory, currentlyInstalledVersion).ConfigureAwait(false);
                    }
                    _logger.LogWarning($"An error occured while downloading ({ex.InnerException?.Message ?? ex.Message}), download aborted");
                    return false;
                }
            }
            finally
            {
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
                var tempDir = tempZipPath.Replace(".tmp", "");
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private string GetCloudConnectorPath(string fccDirectory) => Path.Combine(fccDirectory, "lib", "fiskal-cloud-connector-service.jar");

        private async Task DownloadAndExtractAsync(string tempZipPath, string fccDirectory, Version currentlyInstalledVersion)
        {
            var downloadUri = string.IsNullOrEmpty(_configuration.FccDownloadUri) ? GetDownloadUriByCurrentPlatform() : _configuration.FccDownloadUri;
            _logger.LogDebug("Downloading FCC from {FccDownloadUrl}...", downloadUri);
            using var handler = new HttpClientHandler { Proxy = WebRequest.DefaultWebProxy };
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(_configuration.MaxFccDownloadTimeSec)
            };
            var response = await client.GetAsync(downloadUri);
            using (var fs = new FileStream(tempZipPath, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs).ConfigureAwait(false);
            }
            _logger.LogDebug("Succsfully donwloaded file to {TempFccDownloadFile}. SHA256: {FccSHA256}", tempZipPath, ChecksumHelper.GetSha256FromFile(tempZipPath));
            var fccdataDir = new DirectoryInfo(Path.Combine(fccDirectory, ".fccdata"));
            var baseDir = new DirectoryInfo(fccDirectory);
            if (fccdataDir.Exists)
            {
                // FCC version 4.1.1 requires to delete the existing Java files prior to extracting the new ones
                var versionRequiresJavaUpdate = new Version(4, 1, 1);
                if (currentlyInstalledVersion != null && currentlyInstalledVersion < versionRequiresJavaUpdate && new Version(_configuration.FccVersion) >= versionRequiresJavaUpdate)
                {
                    CleanExistingJavaFiles(fccDirectory);
                    _logger.LogInformation("Cleaned up previous Java installation from /bin folder, as required when updating to FCC >= 4.1.1.");
                }

                UpdateFCCFiles(tempZipPath, fccDirectory, baseDir);
            }
            else
            {
                if (!baseDir.Exists)
                {
                    Directory.CreateDirectory(fccDirectory);
                }
                ZipFile.ExtractToDirectory(tempZipPath, fccDirectory);
                _logger.LogDebug("Extracted FCC to {FccDirectory}", fccDirectory);
            }

            if (EnvironmentHelpers.IsLinux)
            {
                _logger.LogDebug("Linux detected, make FCC files executable...");
                MakeFilesExecutable(fccDirectory);
            }
        }

        private void CleanExistingJavaFiles(string fccDirectory)
        {
            var binDirectory = Path.Combine(fccDirectory, "bin");
            if (Directory.Exists(binDirectory))
            {
                Directory.Delete(binDirectory, true);
            }
        }

        private void UpdateFCCFiles(string tempZipPath, string fccDirectory, DirectoryInfo baseDir)
        {
            var runningProcess = DeutscheFiskalFccProcessHost.GetProcessIfRunning(fccDirectory, _logger);
            if (runningProcess != null)
            {
                _logger.LogInformation("FCC process from {FccPath} has to be shut down to update.", fccDirectory);
                runningProcess.Kill();
                runningProcess.WaitForExit();
                runningProcess.Close();

            }
            foreach (var dir in baseDir.EnumerateDirectories())
            {
                if (dir.Name.ToLower() != ".fccdata" && dir.Name.ToLower() != ".install")
                {
                    Directory.Delete(dir.FullName, true);
                }
            }
            var tempDir = tempZipPath.Replace(".tmp", "");
            ZipFile.ExtractToDirectory(tempZipPath, tempDir);
            var folderDir = new DirectoryInfo(tempDir);
            foreach (var dir in folderDir.EnumerateDirectories())
            {
                var folderPath = Path.Combine(fccDirectory, dir.Name);
                FileSystemHelper.CopyDirectoryRecursively(dir.FullName, folderPath);
                _logger.LogDebug("Updated FCC directory '{dir}'.", dir);
            }
            Directory.Delete(tempDir, true);
        }

        protected virtual string GetDownloadUriByCurrentPlatform()
        {
            var operatingSystem = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "i686",
                Architecture.Arm64 => "aarch64",
                Architecture.Arm => "aarch32hf",
                _ => throw new NotImplementedException($"The processor architecture {RuntimeInformation.ProcessArchitecture} is not supported.")
            };

            string platform;
            if (EnvironmentHelpers.IsWindows)
            {
                platform = "windows";
            }
            else if (EnvironmentHelpers.IsLinux)
            {
                platform = "linux";
            }
            else
            {
                throw new NotSupportedException("The current OS is not supported by this SCU.");
            }
            return $"{DOWNLOAD_DIRECTORY}{NAMEPREFIX}-{_configuration.FccVersion}-{platform}-{operatingSystem}.zip";
        }

        private void MakeFilesExecutable(string fccDirectory)
        {
            var paths = new List<string>
            {
                "./*.sh",
                "./bin/jre/bin/*",
                "./bin/jre/lib/*"
            };

            foreach (var path in paths)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \" chmod a+x {path}\"",
                    WorkingDirectory = fccDirectory,
                    CreateNoWindow = true
                };
                using var proc = new Process { StartInfo = startInfo };
                proc.Start();
                if (!proc.WaitForExit(MAX_PROCESS_DURATION_MS) || proc.ExitCode != 0)
                {
                    _logger.LogError($"Could not run 'chmod u+x' on '{path}'. If you encounter any errors, please make this path executable.");
                }
            }
        }
        private string DeleteLastSlash(string path) => path.TrimEnd('/').TrimEnd('\\');
    }
}
