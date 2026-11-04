using fiskaltrust.Middleware.Journal.DE.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Helpers
{
    public class ConversionTests
    {
        [Theory]
        [InlineData("AT", "AUT")]
        [InlineData("DE", "DEU")]
        [InlineData("CH", "CHE")]
        [InlineData("US", "USA")]
        public void From2DigitsTo3DigitsCountryCode_Should_Return_PropertCountryCodes(string twoDigitCountryCode, string threeDigitCountryCode) 
            => Conversion.From2DigitsTo3DigitsCountryCode(twoDigitCountryCode).Should().Be(threeDigitCountryCode);

        [Theory]
        // Fiskaly
        [InlineData("MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE+rAm8qU9TzDGoJHW7jP0ZnlKiOrSpe4dr1tUHBInYVrySwjgtEM4gNC29OSjxYyJgEKhuxK/UN4L2bWvQzeD5A==", "fc976f6020e0758056b2ac494bcc005bd0ccbb8bf34970ce8bd39ff2bd6c961a")]
        [InlineData("MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE5cAwb8F8T4EEkcxlVohGAIcBWxlGmylmurzch9YDWDIdbVur9RJj4/C5ndksxuY4/nptlvcemfFrqfMn7+GQcg==", "d2c33c20fd78aa1f417ecee1f1d25735ea1129a406422e61c2997a356ec3fc75")]
        // Swissbit
        [InlineData("BDtPHNfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA==", "DF00AC9741FAA7CC03FA80E9A84A969741CADF958F1DBC43D6AFF12F9835FC86")]
        [InlineData("BCVYkaGREqabUp5SneDpm2l4FozGwwoP2XfK4FtEzHv9BR4N+jXDHpu2cmle7+fpFVjwFezwwdOnqig/lOXb64EX0w8n5tuRNMv1tRzulquz5nsf9R5sGklhitPt+WuQhA==", "6B8A60D62A45A2C1808B93215E526278244B826EA42A3E880A8ECFE12782D45E")]
        // CryptoVision
        [InlineData("BAtHmX6Z023R6NdHUy9xFKkmudEkYkar0Xe5fIkfRx3hWXslga4nECXmALuZQcSatk3vQqjJSP1uwD2MRazJFzA=", "CD5C0FC72A87BBC7D5D2BEDCD9202BA4D1E3F20A282D8B04B64506ADE152870D")]
        [InlineData("BOMds0WgrTxee8q8O5VyNZAk2zsJqhUkrQB2PPY/jFf1Ddr3gova6pgcK1CamU4OHi74tgsVKvP2Oh7Om8Btw7E=", "595CE5D7F7621DE401BBB9B5844733832097F818492573056B1CE3983007D80C")]
        public void FromTsePublicKeyToSerialNumber_Should_Return_ExpectedSerials(string publicKeyBase64, string serialNumber)
            => Conversion.FromTsePublicKeyToSerialNumber(publicKeyBase64).Should().Be(serialNumber.ToLower());
    }
}
