using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Localization.QueueDE.Extensions;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueDE.UnitTest.Extensions
{
    public class ReceiptCaseDataExtensionsTests
    {
        [Fact]
        public void GetMarketReceiptCaseData_ReturnsValueUnchanged_ForFlatBaseShape()
        {
            var flat = "{\"RefReceiptId\":\"abc\"}";

            flat.GetMarketReceiptCaseData().Should().Be(flat);
        }

        [Fact]
        public void GetMarketReceiptCaseData_ReturnsMarketSection_ForNestedMappedShape()
        {
            var nested = "{\"DE\":{\"RefReceiptId\":\"abc\"},\"v2ReceiptRequest\":{\"cbReceiptReference\":\"r\"}}";

            var section = nested.GetMarketReceiptCaseData();

            JObject.Parse(section)["RefReceiptId"]!.Value<string>().Should().Be("abc");
            section.Should().NotContain("v2ReceiptRequest");
        }

        [Fact]
        public void GetMarketReceiptCaseData_ReturnsRoot_WhenNoMarketKey_EvenWithV2ReceiptRequest()
        {
            // A flat client request that went through the mapping keeps its fields at the root next to
            // v2ReceiptRequest; the DE fields must still be readable from the root.
            var json = "{\"RefReceiptId\":\"abc\",\"v2ReceiptRequest\":{\"cbReceiptReference\":\"r\"}}";

            json.GetMarketReceiptCaseData().Should().Be(json);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not json")]
        public void GetMarketReceiptCaseData_ReturnsInput_ForNullEmptyOrMalformed(string input)
        {
            input.GetMarketReceiptCaseData().Should().Be(input);
        }

        [Theory]
        // base (flat) and nested (market-keyed) shapes both yield the DE fields
        [InlineData("{\"CurrentStartedTransactionNumbers\":[1,2,3]}")]
        [InlineData("{\"DE\":{\"CurrentStartedTransactionNumbers\":[1,2,3]},\"v2ReceiptRequest\":{\"cbReceiptReference\":\"r\"}}")]
        public void TseInfo_CurrentStartedTransactionNumbers_ReadableFromBothShapes(string caseData)
        {
            var tseInfo = JsonConvert.DeserializeObject<TseInfo>(caseData.GetMarketReceiptCaseData());

            tseInfo.CurrentStartedTransactionNumbers.Should().BeEquivalentTo(new ulong[] { 1, 2, 3 });
        }
    }
}
