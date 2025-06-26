using NUnit.Framework;
using Newtonsoft.Json;
using FluentAssertions;
using System;
using System.Collections.Generic;
using fiskaltrust.storage.serialization.UnitTest.Helper;

namespace fiskaltrust.storage.serialization.UnitTest
{
    public class SerializationUnitTest
    {
        [Test]
        public void SerializeftCashboxConfiguration_PropertiesSet()
        {
            var expectedJson = "{\"helpers\":[{\"Id\":\"f5f3261b-4265-417e-88c2-3d72785def83\",\"Package\":\"\",\"Version\":\"\",\"Configuration\":null,\"Url\":null}],\"ftCashBoxId\":\"6898bef5-618f-40fd-89fe-ed93cadd258e\",\"ftSignaturCreationDevices\":[{\"Id\":\"811ea9f4-64b1-4de7-99fa-0ff7f3956e2a\",\"Package\":\"\",\"Version\":\"\",\"Configuration\":null,\"Url\":null}],\"ftQueues\":[{\"Id\":\"2c047bc6-ec38-4f54-8c7f-f123854b9ea4\",\"Package\":\"\",\"Version\":\"\",\"Configuration\":null,\"Url\":null}],\"TimeStamp\":12345}";

            Guid ftCashBoxId = Guid.Parse("6898BEF5-618F-40FD-89FE-ED93CADD258E");
            var cashbox = new V0.ftCashBoxConfiguration(ftCashBoxId)
            {
                ftQueues = new V0.PackageConfiguration[]{ new V0.PackageConfiguration() { Id = Guid.Parse("2c047bc6-ec38-4f54-8c7f-f123854b9ea4") } },
                ftSignaturCreationDevices = new V0.PackageConfiguration[] { new V0.PackageConfiguration() { Id = Guid.Parse("811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A") } },
                helpers = new V0.PackageConfiguration[] { new V0.PackageConfiguration() { Id = Guid.Parse("F5F3261B-4265-417E-88C2-3D72785DEF83") } },
                TimeStamp = 12345
            };

            var json = JsonConvert.SerializeObject(cashbox);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<V0.ftCashBoxConfiguration>(json);
            deserialized.Should().BeUnorderedEqualTo(cashbox);
        }

        [Test]
        public void SerializeftCashboxConfiguration_SkipIgnoreProperties()
        {
            var expectedJson = "{\"helpers\":null,\"ftCashBoxId\":\"00000000-0000-0000-0000-000000000000\",\"ftSignaturCreationDevices\":[],\"ftQueues\":[],\"TimeStamp\":637025110970215110}";

            var cashbox = new V0.ftCashBoxConfiguration
            {
                TimeStamp = 637025110970215110
            };
            var json = JsonConvert.SerializeObject(cashbox);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<V0.ftCashBoxConfiguration>(json);
            deserialized.Should().BeUnorderedEqualTo(cashbox);
        }

        [Test]
        public void SerializePackageConfiguration_PropertiesSet()
        {
            var expectedJson = "{\"Id\":\"f5f3261b-4265-417e-88c2-3d72785def83\",\"Package\":\"Package\",\"Version\":\"1.2.1\",\"Configuration\":{\"string\":\"Test\",\"int\":123},\"Url\":[\"6898BEF5-618F-40FD-89FE-ED93CADD258E\",\"2c047bc6-ec38-4f54-8c7f-f123854b9ea4\"]}";

            Guid ftCashBoxId = Guid.Parse("6898BEF5-618F-40FD-89FE-ED93CADD258E");
            var packageConfig = new V0.PackageConfiguration()
            {
                Id = Guid.Parse("F5F3261B-4265-417E-88C2-3D72785DEF83"),
                Configuration = new Dictionary<string, object>() { { "string", "Test" }, { "int", 123 } },
                Package = "Package",
                Url = new string[] { "6898BEF5-618F-40FD-89FE-ED93CADD258E", "2c047bc6-ec38-4f54-8c7f-f123854b9ea4" },
                Version = "1.2.1"
            };

            var json = JsonConvert.SerializeObject(packageConfig);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<V0.PackageConfiguration>(json);
            deserialized.Should().BeUnorderedEqualTo(packageConfig);
        }

        [Test]
        public void SerializePackageConfiguration_SkipIgnoreProperties()
        {
            var expectedJson = "{\"Id\":\"00000000-0000-0000-0000-000000000000\",\"Package\":\"\",\"Version\":\"\",\"Configuration\":null,\"Url\":null}";

            var packageConfig = new V0.PackageConfiguration();
            var json = JsonConvert.SerializeObject(packageConfig);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<V0.PackageConfiguration>(json);
            deserialized.Should().BeUnorderedEqualTo(packageConfig);
        }

        [Test]
        public void SerializeTimeStampConfiguration()
        {
            var timeStamp = new V0.TimeStampConfiguration
            {
                TimeStamp = 123456789
            };
            var json = JsonConvert.SerializeObject(timeStamp);
            json.Should().Be("{\"TimeStamp\":123456789}");
            var deserialized = JsonConvert.DeserializeObject<V0.TimeStampConfiguration>(json);
            deserialized.Should().BeUnorderedEqualTo(timeStamp);
        }

    }
}
