using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.IntegrationTest
{
    public class HardwareFixtures
    {
        public HardwareFixtures()
        {
            var mountPoint = Environment.GetEnvironmentVariable("CRYPTOVISION_MOUNTPOINT");
            if (!string.IsNullOrEmpty(mountPoint))
            {
                MountPoint = mountPoint;
            }
        }

        public string MountPoint { get; } = "e:";

        public string AdminName { get; } = "Admin";
        public byte[] AdminPin { get; } = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        public byte[] AdminPuk { get; } = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a };

        public string TimeAdminName { get; } = "TimeAdmin";
        public byte[] TimeAdminPin { get; } = Encoding.UTF8.GetBytes("22222222");
        public byte[] TimeAdminPuk { get; } = Encoding.UTF8.GetBytes("something!");

        public string ClientId { get; } = "fiskaltrust.Middleware";

        public string ProcessTypeSonstigerVorgang = "SonstigerVorgang";
        public string ProcessTypeBestellung = "Bestellung-V1";
        public string ProcessTypeKassenbeleg = "Kassenbeleg-V1";
    }
}
