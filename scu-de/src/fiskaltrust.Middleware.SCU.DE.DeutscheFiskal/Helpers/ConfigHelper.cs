using System;
using System.IO;
using System.Linq;
using static fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants.DeutscheFiskalConstants;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers
{
    public static class ConfigHelper
    {

        public static void SetFccHeapMemory4Run(string fccDirectory, int fccHeapMemory)
        {
            ValidateHeapMemory(fccHeapMemory);
            string runScript;
            if (EnvironmentHelpers.IsWindows)
            {
                runScript = Path.Combine(fccDirectory, Paths.RunFccScriptWindows);
            }
            else if (EnvironmentHelpers.IsLinux)
            {
                runScript = Path.Combine(fccDirectory, Paths.RunFccScriptLinux);
            }
            else
            {
                throw new NotSupportedException("The current OS is not supported by this SCU.");
            }
            var runCommand = File.ReadAllText(Path.Combine(fccDirectory, runScript));
            SetHeapMemory(fccDirectory, fccHeapMemory, runScript, runCommand);
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
