using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services
{
    public sealed class DeutscheFiskalFccProcessHost : IDisposable, IFccProcessHost
    {
        private readonly DeutscheFiskalSCUConfiguration _configuration;
        private readonly ILogger<DeutscheFiskalFccProcessHost> _logger;

        private Process _process;
        private bool _startedProcessInline;

        public DeutscheFiskalFccProcessHost(DeutscheFiskalSCUConfiguration configuration, ILogger<DeutscheFiskalFccProcessHost> logger)
        {
            _configuration = configuration;
            _logger = logger;
            IsExtern = string.IsNullOrEmpty(configuration.FccUri) ? false : true;
        }
        public bool IsRunning => !_process?.HasExited ?? false;
        public bool IsExtern { get; private set; }
        public async Task StartAsync(string fccDirectory)
        {
            _logger.LogInformation("Starting FCC from {FccPath}, this may take a few seconds..", fccDirectory);
            if (!Directory.Exists(fccDirectory))
            {
                throw new DirectoryNotFoundException($"The given fccDirectory '{fccDirectory}' does not exist.");
            }

            var runningProcess = GetProcessIfRunning(fccDirectory, _logger);
            if (runningProcess != null)
            {
                _logger.LogInformation("FCC process from {FccPath} was already running, connected.", fccDirectory);
                _process = runningProcess;
                return;
            }
            try
            {
                _process = new Process
                {
                    StartInfo = GetProcessStartInfo(fccDirectory)
                };
                _process.Start();
                _process.OutputDataReceived += (_, e) => LogFcc(LogLevel.Debug, e?.Data);
                _process.ErrorDataReceived += (_, e) => LogFcc(LogLevel.Error, e?.Data);
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                await WaitUntilFccIsAvailable(DeutscheFiskalConstants.DefaultProcessTimeoutMs);
                _startedProcessInline = true;
            }
            catch (Exception)
            {
                _process?.Kill();
                _process?.Dispose();
                _process = null;
                throw;
            }

            _logger.LogInformation("Succesfully started FCC from {FccPath}.", fccDirectory);
        }

        public static Process GetProcessIfRunning(string fccDirectory, ILogger logger)
        {
            try
            {
                var path = Path.GetFullPath(Path.Combine(fccDirectory, DeutscheFiskalConstants.Paths.EmbeddedJava));
                return Process.GetProcessesByName("java").FirstOrDefault(x => Path.GetFullPath(x.MainModule.FileName).ToLower() == path.ToLower());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occured when trying to bind to an existing FCC process.");
                return null;
            }
        }

        private async Task WaitUntilFccIsAvailable(int timeoutMs)
        {
            var endTime = DateTime.Now.AddMilliseconds(timeoutMs);
            while (DateTime.Now < endTime)
            {
                if (await HttpHelpers.IsAddressAvailable($"http://localhost:{_configuration.FccPort ?? DeutscheFiskalConstants.DefaultPort}/actuator/health"))
                {
                    return;
                }
                await Task.Delay(500);
            }

            throw new TimeoutException($"Starting the FCC service took more than {timeoutMs}ms, hence the process was canceled.");
        }

        public void Dispose()
        {
            if (_startedProcessInline)
            {
                _process.Kill();
            }
            _process?.Dispose();
        }

        private void LogFcc(LogLevel level, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                _logger.Log(level, data);
            }
        }
        private ProcessStartInfo GetProcessStartInfo(string fccDirectory)
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = fccDirectory
            };
            if (EnvironmentHelpers.IsWindows)
            {
                processStartInfo.FileName = Path.Combine(fccDirectory, DeutscheFiskalConstants.Paths.RunFccScriptWindows);
            }
            else if (EnvironmentHelpers.IsLinux)
            {
                processStartInfo.FileName = "sh";
                processStartInfo.Arguments = Path.Combine(fccDirectory, DeutscheFiskalConstants.Paths.RunFccScriptLinux);
            }
            else
            {
                throw new NotSupportedException("The current OS is not supported by this SCU.");
            }
            return processStartInfo;
        }
    }
}
