using Xunit;
using fiskaltrust.Middleware.Localization.QueueAT.Helpers;
using System.Text;
using FluentAssertions;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.Helpers
{
    public class ConversionHelperTests
    {
        [Fact]
        public void ToBase64UrlString_ReturnsCorrectString()
        {
            var examplePayload = "_R1-35_6caa852c-4230-4496-83c0-1597eee7084e_ftA#1_2024-04-16T10:54:07_38,75_0,00_0,00_0,00_0,00_DJ/n9E8=_1_";
            var jWSSignatureBase64url = ConversionHelper.ToBase64UrlString(Encoding.UTF8.GetBytes(examplePayload));
            var result = ConversionHelper.FromBase64UrlString(jWSSignatureBase64url);
            var resultString = Encoding.UTF8.GetString(result);

            resultString.Should().Be(examplePayload);
        }

    }
}
