using System;
using System.Diagnostics;
using System.IO;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Exceptions;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Services
{
    public class DeutscheFiskalFccInitializationService : IFccInitializationService
    {
        private const string FIREWALL_RULE_NAME = "fiskaltrust.Middleware FCC";

        private readonly DeutscheFiskalSCUConfiguration _configuration;
        private readonly ILogger<DeutscheFiskalFccInitializationService> _logger;
        private readonly FirewallHelper _firewallHelper;

        public DeutscheFiskalFccInitializationService(DeutscheFiskalSCUConfiguration configuration, ILogger<DeutscheFiskalFccInitializationService> logger, FirewallHelper firewallHelper)
        {
            _configuration = configuration;
            _logger = logger;
            _firewallHelper = firewallHelper;
        }

        public bool IsInitialized(string fccDirectory) => Directory.Exists(Path.Combine(fccDirectory, ".fccdata", "db"));

        public void Initialize(string fccDirectory)
        {
            _logger.LogInformation("Initializing FCC from {FccPath}, this may take a few seconds..", fccDirectory);
            if (!Directory.Exists(fccDirectory))
            {
                throw new DirectoryNotFoundException($"The given fccDirectory '{fccDirectory}' does not exist.");
            }

            var javaPath = Path.Combine(fccDirectory, DeutscheFiskalConstants.Paths.EmbeddedJava);
            var jarPath = Path.Combine(fccDirectory, DeutscheFiskalConstants.Paths.FccDeployJar);

            AddFirewallExceptionIfApplicable(javaPath);
            var heapMem = GetHeapMemory();

            var arguments = $"-cp \"{jarPath}\" {heapMem} de.fiskal.connector.init.InitializationCLIApplication --fcc_target_environment STABLE --fcc_id {_configuration.FccId} --fcc_secret {_configuration.FccSecret}";
            if (_configuration.FccPort.HasValue)
            {
                arguments += $" --fcc_server_port {_configuration.FccPort.Value}";
            }
            if (!string.IsNullOrEmpty(_configuration.ProxyServer))
            {
                arguments += GetProxyArguments(_configuration);
            }
            RunJavaProcess(fccDirectory, javaPath, arguments);
            _logger.LogInformation("Succesfully initialized FCC from {FccPath}.", fccDirectory);
        }

        private string GetHeapMemory()
        {
            if (!_configuration.FccHeapMemory.HasValue)
            {
                return string.Empty;
            }
            ConfigHelper.ValidateHeapMemory(_configuration.FccHeapMemory.Value);
            return $"-Xmx{_configuration.FccHeapMemory.Value}m";
        }

        public void Update(string fccDirectory)
        {
            _logger.LogInformation("Updating FCC in {FccPath}, this may take a few seconds..", fccDirectory);
            if (!Directory.Exists(fccDirectory))
            {
                throw new DirectoryNotFoundException($"The given fccDirectory '{fccDirectory}' does not exist.");
            }

            var javaPath = Path.Combine(fccDirectory, DeutscheFiskalConstants.Paths.EmbeddedJava);
            var jarPath = Path.Combine(fccDirectory, DeutscheFiskalConstants.Paths.FccDeployJar);

            AddFirewallExceptionIfApplicable(javaPath);

            var heapMem = GetHeapMemory();

            var arguments = $"-cp \"{jarPath}\" {heapMem} de.fiskal.connector.init.UpdateCLIApplication";

            RunJavaProcess(fccDirectory, javaPath, arguments);

            _logger.LogInformation("Succesfully updated FCC in {FccPath}.", fccDirectory);
        }

        private void RunJavaProcess(string fccDirectory, string javaPath, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = fccDirectory
                }
            };
            process.Start();
            var stdout = "";
            process.OutputDataReceived += (_, e) =>
            {
                if (e?.Data != null)
                {
                    stdout += e.Data + Environment.NewLine;
                }
            };
            process.BeginOutputReadLine();

            var hasExited = process.WaitForExit(_configuration.ProcessTimeoutSec * 1000);
            if (!hasExited)
            {
                process.Kill();
                _logger.LogError(stdout);
                throw new TimeoutException($"Initializing or updating the FCC connector took longer than the configured ProcessTimeoutSec {_configuration.ProcessTimeoutSec} seconds, hence the process was canceled. Please refer to the ERROR messages in the FCC logs above to detect the issue.");
            }

            if (process.ExitCode != 0)
            {
                _logger.LogError(stdout);
                throw new FiskalCloudException("An error occured while initializing or updating the Fiskal Cloud Connector. Please refer to the ERROR messages in the FCC logs above to detect the issue.");
            }
            else
            {
                _logger.LogDebug(stdout);
            }
        }

        private void AddFirewallExceptionIfApplicable(string javaPath)
        {
            try
            {
                if (!_configuration.DontAddFccFirewallException && EnvironmentHelpers.IsWindows && !_firewallHelper.RuleExists(FIREWALL_RULE_NAME, javaPath))
                {
                    _logger.LogInformation("Adding rule to Windows Firewall to allow FCC internet access..");
                    _firewallHelper.Allow(FIREWALL_RULE_NAME, javaPath);
                    _logger.LogInformation($"Successfully added Windows Firewall rule '{FIREWALL_RULE_NAME}'");
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning($"The Middleware must be started with administrative privileges to add exceptions to the Windows Firewall.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"An error occured while trying to set the Windows Firewall exception: {ex.Message}.");
            }
        }

        private string GetProxyArguments(DeutscheFiskalSCUConfiguration configuration)
        {
            var arguments = $" --fcc_proxy_enabled 1 --fcc_proxy_server \"{configuration.ProxyServer}\"";
            _logger.LogDebug($"Using FCC proxy server {configuration.ProxyServer}.");

            if (configuration.ProxyPort.HasValue)
            {
                arguments += $" --fcc_proxy_port {configuration.ProxyPort.Value}";
                _logger.LogDebug($"Using FCC proxy port {configuration.ProxyPort.Value}.");
            }
            else
            {
                _logger.LogWarning("'ProxyPort' was not set, falling back to default value 3128.");
                arguments += $" --fcc_proxy_port 3128";
            }

            if (!string.IsNullOrEmpty(configuration.ProxyUsername) && !string.IsNullOrEmpty(configuration.ProxyPassword))
            {
                arguments += $" --fcc_proxy_auth_enabled 1 --fcc_proxy_username \"{configuration.ProxyUsername}\" --fcc_proxy_password \"{configuration.ProxyPassword}\"";
                _logger.LogDebug($"Using FCC proxy username '{configuration.ProxyUsername}' and password '******'.");
            }
            else
            {
                _logger.LogDebug("No value set for 'ProxyUsername' and/or 'ProxyPassword'. Using anonymous proxy authentication.");
            }

            return arguments;
        }
    }
}
