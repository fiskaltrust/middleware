using System;
using System.IO;
using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.IntegrationTest
{
    public class ConfigHelperTest
    {
        [Fact]
        public void ConfigHelperTest_NoXmx_SetXmx()
        {
            var fccRunNoXms = Path.Combine(Path.Combine(GetDirectoryPath(), "testdata", "NoXmx"));
            ConfigHelper.SetFccHeapMemoryForRunScript(fccRunNoXms, 1024);
            var runCommand = File.ReadAllText(Path.Combine(fccRunNoXms, "run_fcc.bat"));
            var pos1024 = runCommand.IndexOf("-Xmx1024m");
            pos1024.Should().NotBe(-1);
        }
        [Fact]
        public void ConfigHelperTest_Xmx1024_replaceXmxTo512()
        {
            var fccRunNoXms = Path.Combine(Path.Combine(GetDirectoryPath(), "testdata", "replaceXmx1024"));
            ConfigHelper.SetFccHeapMemoryForRunScript(fccRunNoXms, 512);
            var runCommand = File.ReadAllText(Path.Combine(fccRunNoXms, "run_fcc.bat"));
            var pos1024 = runCommand.IndexOf("-Xmx512m");
            pos1024.Should().NotBe(-1);
        }
        [Fact]
        public void ConfigHelperTest_Xmx512_replaceXmxTo1024()
        {
            var fccRunNoXms = Path.Combine(Path.Combine(GetDirectoryPath(), "testdata", "replaceXmx512"));
            ConfigHelper.SetFccHeapMemoryForRunScript(fccRunNoXms, 1024);
            var runCommand = File.ReadAllText(Path.Combine(fccRunNoXms, "run_fcc.bat"));
            var pos1024 = runCommand.IndexOf("-Xmx1024m");
            pos1024.Should().NotBe(-1);
        }
        public static string GetDirectoryPath()
        {
            var filePath = new Uri(typeof(ConfigHelperTest).Assembly.Location).LocalPath;
            return Path.GetDirectoryName(filePath);
        }
    }
}
