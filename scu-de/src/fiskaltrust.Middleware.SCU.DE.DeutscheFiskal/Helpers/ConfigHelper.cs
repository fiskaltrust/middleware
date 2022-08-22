using System;
using System.IO;
using System.Linq;
using static fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants.DeutscheFiskalConstants;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers
{
    public static class ConfigHelper
    {

        public static void SetFccHeapMemory(string fccDirectory, int fccHeapMemory)
        {
            var validXms = new int[] { 256, 512, 1024 };
            if (!validXms.Contains(fccHeapMemory))
            {
                throw new Exception($"Invalid Value in FccHeapMemory{fccHeapMemory}. Posibble values: 256, 512, 1024.");
            }
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

            var posOfX = runCommand.IndexOf("-Xmx");
            if (posOfX >= 0)
            {
                var fromXms = runCommand.Substring(posOfX);
                var posM = fromXms.IndexOf("m", 4);
                var xms = fromXms.Substring(0, posM+1);
                if (!xms.Equals($"-Xmx{fccHeapMemory}m"))
                {
                    runCommand = runCommand.Replace(xms, $"-Xmx{fccHeapMemory}m");
                    File.WriteAllText(Path.Combine(fccDirectory, runScript), runCommand);
                }
            }
            else
            {
                var posJar = runCommand.IndexOf(Paths.FccServiceJar);
                runCommand = runCommand.Insert(posJar + Paths.FccServiceJar.Length + 1, $"-Xmx{fccHeapMemory}m ");
                File.WriteAllText(Path.Combine(fccDirectory, runScript), runCommand);
            }
        }
    }
}
