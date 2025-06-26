using NUnit.Framework;
using Newtonsoft.Json;
using FluentAssertions;
using System;
using fiskaltrust.storage.serialization.UnitTest.Helper;

namespace fiskaltrust.storage.serialization.UnitTest
{
    public class ATSerializationUnitTest
    {
        [Test]
        public void SerializeFonActiveQueue_PropertiesSet()
        {
            var expectedJson = "{\"CashBoxId\":\"6898bef5-618f-40fd-89fe-ed93cadd258e\",\"QueueId\":\"2c047bc6-ec38-4f54-8c7f-f123854b9ea4\",\"Moment\":\"2018-08-27T13:54:26\",\"CashBoxIdentification\":\"811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A\",\"CashBoxKeyBase64\":\"F5F3261B-4265-417E-88C2-3D72785DEF83\",\"Note\":\"Test\",\"ClosedSystemKind\":\"ClosedSystemKind\",\"ClosedSystemValue\":\"ClosedSystemValue\",\"DEPValue\":\"DEPValue\",\"IsStartReceipt\":false,\"Version\":\"1.2.1\"}";

            var queue = new AT.V0.FonActivateQueue()
            {
                CashBoxId = Guid.Parse("6898BEF5-618F-40FD-89FE-ED93CADD258E"),
                CashBoxIdentification = "811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A",
                CashBoxKeyBase64 = "F5F3261B-4265-417E-88C2-3D72785DEF83",
                ClosedSystemKind = "ClosedSystemKind",
                ClosedSystemValue = "ClosedSystemValue",
                DEPValue = "DEPValue",
                IsStartReceipt = false,
                Moment = new DateTime(2018, 8, 27, 13, 54, 26),
                Note = "Test",
                QueueId = Guid.Parse("2C047BC6-EC38-4F54-8C7F-F123854B9EA4"),
                Version = "1.2.1",
            };

            var json = JsonConvert.SerializeObject(queue);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonActivateQueue>(json);
            deserialized.Should().BeUnorderedEqualTo(queue);
        }

        [Test]
        public void SerializeFonActiveQueue_SkipIgnoreProperties()
        {
            var expectedJson = "{\"CashBoxId\":\"00000000-0000-0000-0000-000000000000\",\"QueueId\":\"00000000-0000-0000-0000-000000000000\",\"Moment\":\"0001-01-01T00:00:00\",\"CashBoxIdentification\":null,\"DEPValue\":null,\"IsStartReceipt\":false}";

            var queue = new AT.V0.FonActivateQueue();
            var json = JsonConvert.SerializeObject(queue);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonActivateQueue>(json);
            deserialized.Should().BeUnorderedEqualTo(queue);
        }

        [Test]
        public void SerializeFonDeactiveQueue_PropertiesSet()
        {
            var expectedJson = "{\"CashBoxId\":\"6898bef5-618f-40fd-89fe-ed93cadd258e\",\"QueueId\":\"2c047bc6-ec38-4f54-8c7f-f123854b9ea4\",\"Moment\":\"2018-08-27T13:54:26\",\"CashBoxIdentification\":\"811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A\",\"ClosedSystemKind\":\"ClosedSystemKind\",\"ClosedSystemValue\":\"ClosedSystemValue\",\"Note\":\"Test\",\"DEPValue\":\"DEPValue\",\"IsStopReceipt\":false,\"Version\":\"1.2.1\"}";

            var queue = new AT.V0.FonDeactivateQueue()
            {
                CashBoxId = Guid.Parse("6898BEF5-618F-40FD-89FE-ED93CADD258E"),
                CashBoxIdentification = "811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A",
                ClosedSystemKind = "ClosedSystemKind",
                ClosedSystemValue = "ClosedSystemValue",
                DEPValue = "DEPValue",
                Moment = new DateTime(2018, 8, 27, 13, 54, 26),
                Note = "Test",
                QueueId = Guid.Parse("2C047BC6-EC38-4F54-8C7F-F123854B9EA4"),
                IsStopReceipt = false,
                Version = "1.2.1",
            };

            var json = JsonConvert.SerializeObject(queue);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonDeactivateQueue>(json);
            deserialized.Should().BeUnorderedEqualTo(queue);
        }

        [Test]
        public void SerializeFonDeactiveQueue_SkipIgnoreProperties()
        {
            var expectedJson = "{\"CashBoxId\":\"00000000-0000-0000-0000-000000000000\",\"QueueId\":\"00000000-0000-0000-0000-000000000000\",\"Moment\":\"0001-01-01T00:00:00\",\"CashBoxIdentification\":null,\"DEPValue\":null,\"IsStopReceipt\":false}";

            var queue = new AT.V0.FonDeactivateQueue();
            var json = JsonConvert.SerializeObject(queue);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonDeactivateQueue>(json);
            deserialized.Should().BeUnorderedEqualTo(queue);
        }

        [Test]
        public void SerializeFonActiveSCU_PropertiesSet()
        {
            var expectedJson = "{\"CashBoxId\":\"6898bef5-618f-40fd-89fe-ed93cadd258e\",\"SCUId\":\"2c047bc6-ec38-4f54-8c7f-f123854b9ea4\",\"PackageName\":\"Package\",\"Moment\":\"2018-08-27T13:54:26\",\"VDA\":\"6C6BE085-5EC8-4217-8F9D-1884BF132023\",\"SerialNumber\":\"811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A\",\"CertificateBase64\":\"F5F3261B-4265-417E-88C2-3D72785DEF83\",\"ClosedSystemKind\":\"ClosedSystemKind\",\"ClosedSystemValue\":\"ClosedSystemValue\",\"Note\":\"Test\",\"Version\":\"1.2.1\"}";

            var scu = new AT.V0.FonActivateSCU()
            {
                CashBoxId = Guid.Parse("6898BEF5-618F-40FD-89FE-ED93CADD258E"),
                CertificateBase64 = "F5F3261B-4265-417E-88C2-3D72785DEF83",
                ClosedSystemKind = "ClosedSystemKind",
                ClosedSystemValue = "ClosedSystemValue",
                Moment = new DateTime(2018, 8, 27, 13, 54, 26),
                Note = "Test",
                PackageName = "Package",
                SCUId = Guid.Parse("2C047BC6-EC38-4F54-8C7F-F123854B9EA4"),
                SerialNumber = "811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A",
                VDA = "6C6BE085-5EC8-4217-8F9D-1884BF132023",
                Version = "1.2.1",
            };

            var json = JsonConvert.SerializeObject(scu);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonActivateSCU>(json);
            deserialized.Should().BeUnorderedEqualTo(scu);
        }

        [Test]
        public void SerializeFonActiveSCU_SkipIgnoreProperties()
        {
            var expectedJson = "{\"CashBoxId\":\"00000000-0000-0000-0000-000000000000\",\"SCUId\":\"00000000-0000-0000-0000-000000000000\",\"PackageName\":null,\"Moment\":\"0001-01-01T00:00:00\",\"VDA\":null,\"SerialNumber\":null}";

            var scu = new AT.V0.FonActivateSCU();
            var json = JsonConvert.SerializeObject(scu);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonActivateSCU>(json);
            deserialized.Should().BeUnorderedEqualTo(scu);
        }

        [Test]
        public void SerializeFonDeactiveSCU_PropertiesSet()
        {
            var expectedJson = "{\"CashBoxId\":\"6898bef5-618f-40fd-89fe-ed93cadd258e\",\"SCUId\":\"2c047bc6-ec38-4f54-8c7f-f123854b9ea4\",\"PackageName\":\"Package\",\"Moment\":\"2018-08-27T13:54:26\",\"VDA\":\"6C6BE085-5EC8-4217-8F9D-1884BF132023\",\"SerialNumber\":\"811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A\",\"Temporary\":false,\"CertificateBase64\":\"F5F3261B-4265-417E-88C2-3D72785DEF83\",\"ClosedSystemKind\":\"ClosedSystemKind\",\"ClosedSystemValue\":\"ClosedSystemValue\",\"Note\":\"Test\",\"Version\":\"1.2.1\"}";

            var scu = new AT.V0.FonDeactivateSCU()
            {
                CashBoxId = Guid.Parse("6898BEF5-618F-40FD-89FE-ED93CADD258E"),
                CertificateBase64 = "F5F3261B-4265-417E-88C2-3D72785DEF83",
                ClosedSystemKind = "ClosedSystemKind",
                ClosedSystemValue = "ClosedSystemValue",
                Moment = new DateTime(2018, 8, 27, 13, 54, 26),
                Note = "Test",
                PackageName = "Package",
                SCUId = Guid.Parse("2C047BC6-EC38-4F54-8C7F-F123854B9EA4"),
                SerialNumber = "811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A",
                Temporary = false,
                VDA = "6C6BE085-5EC8-4217-8F9D-1884BF132023",
                Version = "1.2.1"
            };

            var json = JsonConvert.SerializeObject(scu);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonDeactivateSCU>(json);
            deserialized.Should().BeUnorderedEqualTo(scu);
        }

        [Test]
        public void SerializeFonDeactiveSCU_SkipIgnoreProperties()
        {
            var expectedJson = "{\"CashBoxId\":\"00000000-0000-0000-0000-000000000000\",\"SCUId\":\"00000000-0000-0000-0000-000000000000\",\"PackageName\":null,\"Moment\":\"0001-01-01T00:00:00\",\"VDA\":null,\"SerialNumber\":null,\"Temporary\":false}";

            var scu = new AT.V0.FonDeactivateSCU();
            var json = JsonConvert.SerializeObject(scu);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonDeactivateSCU>(json);
            deserialized.Should().BeUnorderedEqualTo(scu);
        }

        [Test]
        public void SerializeFonVerifySignature_PropertiesSet()
        {
            var expectedJson = "{\"CashBoxId\":\"6898bef5-618f-40fd-89fe-ed93cadd258e\",\"QueueId\":\"0c871f62-78dc-4403-8c2d-a82db496f1c1\",\"DEPValue\":\"DEPValue\",\"ClosedSystemKind\":\"ClosedSystemKind\",\"ClosedSystemValue\":\"ClosedSystemValue\",\"SCUId\":\"2c047bc6-ec38-4f54-8c7f-f123854b9ea4\",\"CertificateBase64\":\"F5F3261B-4265-417E-88C2-3D72785DEF83\",\"CashBoxIdentification\":\"811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A\",\"CashBoxKeyBase64\":\"6C6BE085-5EC8-4217-8F9D-1884BF132023\",\"Version\":\"1.2.1\"}";

            var signature = new AT.V0.FonVerifySignature()
            {
                CashBoxId = Guid.Parse("6898BEF5-618F-40FD-89FE-ED93CADD258E"),
                CashBoxIdentification = "811EA9F4-64B1-4DE7-99FA-0FF7F3956E2A",
                CashBoxKeyBase64 = "6C6BE085-5EC8-4217-8F9D-1884BF132023",
                CertificateBase64 = "F5F3261B-4265-417E-88C2-3D72785DEF83",
                ClosedSystemKind = "ClosedSystemKind",
                ClosedSystemValue = "ClosedSystemValue",
                DEPValue = "DEPValue",
                QueueId = Guid.Parse("0C871F62-78DC-4403-8C2D-A82DB496F1C1"),
                SCUId = Guid.Parse("2C047BC6-EC38-4F54-8C7F-F123854B9EA4"),
                Version = "1.2.1"
            };

            var json = JsonConvert.SerializeObject(signature);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonVerifySignature>(json);
            deserialized.Should().BeUnorderedEqualTo(signature);
        }

        [Test]
        public void SerializeFonVerifySignature_SkipIgnoreProperties()
        {
            var expectedJson = "{\"DEPValue\":null}";

            var signature = new AT.V0.FonVerifySignature();
            var json = JsonConvert.SerializeObject(signature);
            json.Should().Be(expectedJson);
            var deserialized = JsonConvert.DeserializeObject<AT.V0.FonVerifySignature>(json);
            deserialized.Should().BeUnorderedEqualTo(signature);
        }
    }
}
