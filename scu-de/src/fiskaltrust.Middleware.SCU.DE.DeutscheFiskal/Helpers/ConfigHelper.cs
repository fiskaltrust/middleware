using System;
using System.IO;
using System.Linq;
using static fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants.DeutscheFiskalConstants;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers
{
    public static class ConfigHelper
    {
        public static void SetConfiguration(DeutscheFiskalSCUConfiguration configuration, string fccDirectory)
        {
            if (configuration.FccHeapMemory.HasValue)
            {
                SetFccHeapMemoryForRunScript(fccDirectory, configuration.FccHeapMemory.Value);
            }
            SetFccMetrics(configuration, fccDirectory);
        }

        private static void SetFccMetrics(DeutscheFiskalSCUConfiguration configuration, string fccDirectory)
        {
            var runScript = GetRunscript(fccDirectory);
            var runCommand = File.ReadAllText(Path.Combine(fccDirectory, runScript));
            var metricsCommand = "-Dspring.profiles.active=metrics ";
            var posOfX = runCommand.IndexOf(metricsCommand);
            if (posOfX >= 0)
            {
                if (configuration.EnableFccMetrics)
                {
                    return;
                }
                runCommand = runCommand.Replace(metricsCommand, "");
            }
            else
            {
                if (!configuration.EnableFccMetrics)
                {
                    return;
                }
                var posJar = runCommand.IndexOf(Paths.FccServiceJar);
                runCommand = runCommand.Insert(posJar + Paths.FccServiceJar.Length + 1, metricsCommand);
            }
            File.WriteAllText(Path.Combine(fccDirectory, runScript), runCommand);

        }

        private static void SetFccHeapMemoryForRunScript(string fccDirectory, int fccHeapMemory)
        {
            ValidateHeapMemory(fccHeapMemory);
            var runScript = GetRunscript(fccDirectory);
            var runCommand = File.ReadAllText(Path.Combine(fccDirectory, runScript));
            SetHeapMemory(fccDirectory, fccHeapMemory, runScript, runCommand);
        }

        private static string GetRunscript(string fccDirectory)
        {
            if (EnvironmentHelpers.IsWindows)
            {
                return Path.Combine(fccDirectory, Paths.RunFccScriptWindows);
            }
            else if (EnvironmentHelpers.IsLinux)
            {
                return Path.Combine(fccDirectory, Paths.RunFccScriptLinux);
            }
            else
            {
                throw new NotSupportedException("The current OS is not supported by this SCU.");
            }
        }

        private static void SetHeapMemory(string fccDirectory, int fccHeapMemory, string script, string command)
        {
            var posOfX = command.IndexOf("-Xmx");
            if (posOfX >= 0)
            {
                var fromXms = command.Substring(posOfX);
                var posM = fromXms.IndexOf("m", 4);
                var xms = fromXms.Substring(0, posM + 1);
                if (!xms.Equals($"-Xmx{fccHeapMemory}m"))
                {
                    command = command.Replace(xms, $"-Xmx{fccHeapMemory}m");
                    File.WriteAllText(Path.Combine(fccDirectory, script), command);
                }
            }
            else
            {
                var posJar = command.IndexOf(Paths.FccServiceJar);
                command = command.Insert(posJar + Paths.FccServiceJar.Length + 1, $"-Xmx{fccHeapMemory}m ");
                File.WriteAllText(Path.Combine(fccDirectory, script), command);
            }
        }

        public static void ValidateHeapMemory(int fccHeapMemory)
        {
            var validXms = new int[] { 256, 512, 1024 };
            if (!validXms.Contains(fccHeapMemory))
            {
                throw new Exception($"Invalid Value in FccHeapMemory{fccHeapMemory}. Posibble values: 256, 512, 1024.");
            }
        }
    }
}
