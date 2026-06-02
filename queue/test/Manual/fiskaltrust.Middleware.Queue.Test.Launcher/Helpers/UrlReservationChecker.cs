using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using fiskaltrust.storage.serialization.V0;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.Middleware.Queue.Test.Launcher.Helpers
{
    /// <summary>
    /// Verifies that http/https URLs configured for the queues (e.g. retrieved from Helipad)
    /// have a matching netsh URL ACL reservation, so hosting does not fail at runtime.
    /// </summary>
    public static class UrlReservationChecker
    {
        public static void CheckQueueUrlReservations(ftCashBoxConfiguration cashBoxConfiguration, ILogger logger)
        {
            if (cashBoxConfiguration?.ftQueues == null)
            {
                return;
            }

            // Already elevated: HttpListener can self-register, no need to warn.
            if (IsRunningAsAdministrator())
            {
                return;
            }

            string aclOutput = null;
            var checkedPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var queue in cashBoxConfiguration.ftQueues)
            {
                if (queue?.Url == null)
                {
                    continue;
                }

                foreach (var rawUrl in queue.Url)
                {
                    if (string.IsNullOrWhiteSpace(rawUrl))
                    {
                        continue;
                    }

                    var httpUrl = rawUrl.Replace("rest://", "http://");
                    if (!Uri.TryCreate(httpUrl, UriKind.Absolute, out var uri))
                    {
                        continue;
                    }

                    if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var key = uri.Scheme.ToLowerInvariant() + ":" + uri.Port.ToString(CultureInfo.InvariantCulture);
                    if (!checkedPorts.Add(key))
                    {
                        continue;
                    }

                    aclOutput ??= QueryUrlAcls();

                    if (HasReservationForPort(aclOutput, uri.Scheme, uri.Port))
                    {
                        logger.LogDebug("URL reservation found for {Scheme}://+:{Port}/", uri.Scheme, uri.Port);
                        continue;
                    }

                    var urlPrefix = $"{uri.Scheme}://+:{uri.Port}{uri.AbsolutePath}";
                    if (!urlPrefix.EndsWith("/", StringComparison.Ordinal))
                    {
                        urlPrefix += "/";
                    }

                    var userName = WindowsIdentity.GetCurrent().Name;
                    logger.LogWarning(
                        "No URL reservation found for port {Port} (configured URL: {Url}). Run the following command once as administrator: netsh http add urlacl url={UrlPrefix} user={User}",
                        uri.Port, rawUrl, urlPrefix, userName);
                }
            }
        }

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static string QueryUrlAcls()
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", "http show urlacl")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(5000);
                return output ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool HasReservationForPort(string aclOutput, string scheme, int port)
        {
            if (string.IsNullOrEmpty(aclOutput))
            {
                return false;
            }

            var portToken = ":" + port.ToString(CultureInfo.InvariantCulture) + "/";
            foreach (var rawLine in aclOutput.Split('\n'))
            {
                var line = rawLine.Trim();
                var schemeIdx = line.IndexOf(scheme + "://", StringComparison.OrdinalIgnoreCase);
                if (schemeIdx < 0)
                {
                    continue;
                }
                if (line.IndexOf(portToken, schemeIdx, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
