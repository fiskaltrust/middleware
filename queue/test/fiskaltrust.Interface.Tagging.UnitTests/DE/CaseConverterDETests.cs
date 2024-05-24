using Xunit;
using fiskaltrust.Interface.Tagging.DE;
using fiskaltrust.Interface.Tagging.Interfaces;
using FluentAssertions;
using fiskaltrust.ifPOS.v1;
namespace fiskaltrust.Interface.Tagging.UnitTests.DE
{
    public class CaseConverterDETests
    {
        private readonly ICaseConverter _caseConverterDE;
        public CaseConverterDETests()
        {
            _caseConverterDE = new CaseConverterDE();
        }
        [Fact]
        public void ConvertftReceiptCaseToV1_ShouldreturnCorrect()
        {
            var request = new ReceiptRequest { ftReceiptCase = 0x4445200000010002 };
            _caseConverterDE.ConvertftReceiptCaseToV1(request);

            request.ftReceiptCase.Should().Be(0x4445000000010011);
        }
    }
}