using System.Text;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.IntegrationTest
{
    public class SwissbitHardwareFixture
    {
        public string ProcessTypeSonstigerVorgang { get; } = "SonstigerVorgang";
        public string ProcessTypeBestellung { get; } = "Bestellung-V1";
        public string ProcessTypeKassenbeleg { get; } = "Kassenbeleg-V1";
        public string MountPoint { get; } = "s:";
        public readonly byte[] Seed = Encoding.ASCII.GetBytes("SwissbitSwissbit");
        public byte[] AdminPin { get; } = Encoding.ASCII.GetBytes("12345");
        public byte[] AdminPuk { get; } = Encoding.ASCII.GetBytes("123456");
        public byte[] TimeAdminPin { get; } = Encoding.ASCII.GetBytes("98765");
        public string ClientId { get; } = "fiskaltrust.Middleware";
    }
}
